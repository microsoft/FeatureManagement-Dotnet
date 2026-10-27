// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class VariantConfigurationCacheTest
    {
        [Fact]
        public void GetOrAddCachesNull()
        {
            VariantConfigurationCache cache = CreateCache();
            int factoryCalls = 0;

            Assert.Null(cache.GetOrAdd<string>(() =>
            {
                factoryCalls++;
                return null;
            }));
            Assert.Null(cache.GetOrAdd<string>(() =>
            {
                factoryCalls++;
                return "unexpected";
            }));
            Assert.Equal(1, factoryCalls);
        }

        [Fact]
        public void GetOrAddRetriesAfterFailure()
        {
            VariantConfigurationCache cache = CreateCache();
            var expected = new object();
            int factoryCalls = 0;

            Assert.Throws<InvalidOperationException>(() => cache.GetOrAdd<object>(() =>
            {
                factoryCalls++;
                throw new InvalidOperationException();
            }));

            Assert.Same(expected, cache.GetOrAdd(() =>
            {
                factoryCalls++;
                return expected;
            }));
            Assert.Equal(2, factoryCalls);
        }

        [Fact]
        public async Task GetOrAddInvokesFactoryOnceForConcurrentCalls()
        {
            const int CallerCount = 8;
            VariantConfigurationCache cache = CreateCache();
            var expected = new object();
            using var callState = new ConcurrentCacheCallState(cache, expected, CallerCount);
            var calls = new Task<object>[CallerCount];

            for (int i = 0; i < calls.Length; i++)
            {
                calls[i] = Task.Factory.StartNew(
                    state => ((ConcurrentCacheCallState)state).GetConfiguration(),
                    callState,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            bool callersReady = callState.CallersReady.Wait(TimeSpan.FromSeconds(10));
            callState.StartCallers.Set();
            bool callersAtCache = callState.CallersAtCache.Wait(TimeSpan.FromSeconds(10));

            object[] results = await Task.WhenAll(calls);

            Assert.True(callersReady);
            Assert.True(callersAtCache);
            Assert.Equal(1, callState.FactoryCalls);
            Assert.False(callState.AdditionalFactoryStarted);
            Assert.All(results, result => Assert.Same(expected, result));
        }

        private static VariantConfigurationCache CreateCache()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            return new VariantConfigurationCache(configuration.GetSection("unused"));
        }

        private sealed class ConcurrentCacheCallState : IDisposable
        {
            private readonly VariantConfigurationCache _cache;
            private readonly object _expected;
            private readonly ManualResetEventSlim _additionalFactoryStarted = new ManualResetEventSlim();
            private int _factoryCalls;

            public ConcurrentCacheCallState(VariantConfigurationCache cache, object expected, int callerCount)
            {
                _cache = cache;
                _expected = expected;
                CallersReady = new CountdownEvent(callerCount);
                CallersAtCache = new CountdownEvent(callerCount);
                StartCallers = new ManualResetEventSlim();
            }

            public CountdownEvent CallersReady { get; }

            public CountdownEvent CallersAtCache { get; }

            public ManualResetEventSlim StartCallers { get; }

            public int FactoryCalls => Volatile.Read(ref _factoryCalls);

            public bool AdditionalFactoryStarted => _additionalFactoryStarted.IsSet;

            public object GetConfiguration()
            {
                CallersReady.Signal();
                StartCallers.Wait();
                CallersAtCache.Signal();

                return _cache.GetOrAdd(() =>
                {
                    int invocation = Interlocked.Increment(ref _factoryCalls);

                    if (invocation == 1)
                    {
                        _additionalFactoryStarted.Wait(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        _additionalFactoryStarted.Set();
                    }

                    return _expected;
                });
            }

            public void Dispose()
            {
                CallersReady.Dispose();
                CallersAtCache.Dispose();
                StartCallers.Dispose();
                _additionalFactoryStarted.Dispose();
            }
        }
    }
}
