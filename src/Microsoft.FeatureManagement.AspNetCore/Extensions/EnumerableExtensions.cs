// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    static class EnumerableExtensions
    {
        public static async Task<bool> Any<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            bool enabled = false;

            foreach (TSource item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                {
                    enabled = true;

                    break;
                }
            }

            return enabled;
        }

        public static async Task<bool> All<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicate)
        {
            bool enabled = true;

            foreach (TSource item in source)
            {
                if (!await predicate(item).ConfigureAwait(false))
                {
                    enabled = false;

                    break;
                }
            }

            return enabled;
        }
    }
}
