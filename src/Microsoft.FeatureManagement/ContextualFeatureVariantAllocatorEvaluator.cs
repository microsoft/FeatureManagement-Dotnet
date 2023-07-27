// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a performance efficient method of evaluating <see cref="IContextualFeatureVariantAllocator{TContext}"/> without knowing what the generic type parameter is.
    /// </summary>
    sealed class ContextualFeatureVariantAllocatorEvaluator : IContextualFeatureVariantAllocator<object>
    {
        private IFeatureVariantAllocatorMetadata _allocator;
        private Func<object, FeatureVariantAllocationContext, object, CancellationToken, ValueTask<FeatureVariant>> _evaluateFunc;

        public ContextualFeatureVariantAllocatorEvaluator(IFeatureVariantAllocatorMetadata allocator, Type appContextType)
        {
            if (allocator == null)
            {
                throw new ArgumentNullException(nameof(allocator));
            }

            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            Type targetInterface = GetContextualAllocatorInterface(allocator, appContextType);

            //
            // Extract IContextualFeatureVariantAllocator<T>.AllocateVariantAsync method.
            if (targetInterface != null)
            {
                MethodInfo evaluateMethod = targetInterface.GetMethod(nameof(IContextualFeatureVariantAllocator<object>.AllocateVariantAsync), BindingFlags.Public | BindingFlags.Instance);

                _evaluateFunc = TypeAgnosticEvaluate(allocator.GetType(), evaluateMethod);
            }

            _allocator = allocator;
        }

        public ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext allocationContext, object context, CancellationToken cancellationToken)
        {
            if (allocationContext == null)
            {
                throw new ArgumentNullException(nameof(allocationContext));
            }

            if (_evaluateFunc == null)
            {
                return new ValueTask<FeatureVariant>((FeatureVariant)null);
            }

            return _evaluateFunc(_allocator, allocationContext, context, cancellationToken);
        }

        public static bool IsContextualVariantAllocator(IFeatureVariantAllocatorMetadata allocator, Type appContextType)
        {
            if (allocator == null)
            {
                throw new ArgumentNullException(nameof(allocator));
            }

            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            return GetContextualAllocatorInterface(allocator, appContextType) != null;
        }

        private static Type GetContextualAllocatorInterface(IFeatureVariantAllocatorMetadata allocator, Type appContextType)
        {
            IEnumerable<Type> contextualAllocatorInterfaces = allocator.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(IContextualFeatureVariantAllocator<>)));

            Type targetInterface = null;

            if (contextualAllocatorInterfaces != null)
            {
                targetInterface = contextualAllocatorInterfaces.FirstOrDefault(i => i.GetGenericArguments()[0].IsAssignableFrom(appContextType));
            }

            return targetInterface;
        }

        private static Func<object, FeatureVariantAllocationContext, object, CancellationToken, ValueTask<FeatureVariant>> TypeAgnosticEvaluate(Type allocatorType, MethodInfo method)
        {
            //
            // Get the generic version of the evaluation helper method
            MethodInfo genericHelper = typeof(ContextualFeatureVariantAllocatorEvaluator).GetMethod(nameof(GenericTypeAgnosticEvaluate),
                BindingFlags.Static | BindingFlags.NonPublic);

            //
            // Create a type specific version of the evaluation helper method
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod
                (allocatorType,
                method.GetParameters()[0].ParameterType,
                method.GetParameters()[1].ParameterType,
                method.GetParameters()[2].ParameterType,
                method.ReturnType);

            //
            // Invoke the method to get the func
            object typeAgnosticDelegate = constructedHelper.Invoke(null, new object[] { method });

            return (Func<object, FeatureVariantAllocationContext, object, CancellationToken, ValueTask<FeatureVariant>>)typeAgnosticDelegate;
        }

        private static Func<object, FeatureVariantAllocationContext, object, CancellationToken, ValueTask<FeatureVariant>> GenericTypeAgnosticEvaluate<TTarget, TParam1, TParam2, TParam3, TReturn>(MethodInfo method)
        {
            Func<TTarget, FeatureVariantAllocationContext, TParam2, CancellationToken, ValueTask<FeatureVariant>> func =
                (Func<TTarget, FeatureVariantAllocationContext, TParam2, CancellationToken, ValueTask<FeatureVariant>>)
                Delegate.CreateDelegate(typeof(Func<TTarget, FeatureVariantAllocationContext, TParam2, CancellationToken, ValueTask<FeatureVariant>>), method);

            Func<object, FeatureVariantAllocationContext, object, CancellationToken, ValueTask<FeatureVariant>> genericDelegate =
                (object target, FeatureVariantAllocationContext param1, object param2, CancellationToken param3) =>
                    func((TTarget)target, param1, (TParam2)param2, param3);

            return genericDelegate;
        }
    }
}
