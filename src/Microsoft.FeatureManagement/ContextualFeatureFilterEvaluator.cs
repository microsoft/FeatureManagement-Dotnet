// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a performance efficient method of evaluating IContextualFeatureFilter&lt;T&gt; without knowing what the generic type parameter is.
    /// </summary>
    class ContextualFeatureFilterEvaluator : IContextualFeatureFilter<object>
    {
        private IFeatureFilterMetadata _filter;
        private Func<object, FeatureFilterEvaluationContext, object, Task<bool>> _evaluateFunc;

        public ContextualFeatureFilterEvaluator(IFeatureFilterMetadata filter, Type appContextType)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            Type targetInterface = GetContextualFilterInterface(filter, appContextType);

            //
            // Extract IContextualFeatureFilter<T>.EvaluateAsync method.
            if (targetInterface != null)
            {
                MethodInfo evaluateMethod = targetInterface.GetMethod(nameof(IContextualFeatureFilter<object>.EvaluateAsync), BindingFlags.Public | BindingFlags.Instance);

                _evaluateFunc = TypeAgnosticEvaluate(filter.GetType(), evaluateMethod);
            }

            _filter = filter;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext evaluationContext, object context)
        {
            if (_evaluateFunc == null)
            {
                return Task.FromResult(false);
            }

            return _evaluateFunc(_filter, evaluationContext, context);
        }

        public static bool IsContextualFilter(IFeatureFilterMetadata filter, Type appContextType)
        {
            return GetContextualFilterInterface(filter, appContextType) != null;
        }

        private static Type GetContextualFilterInterface(IFeatureFilterMetadata filter, Type appContextType)
        {
            IEnumerable<Type> contextualFilterInterfaces = filter.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(IContextualFeatureFilter<>)));

            Type targetInterface = null;

            if (contextualFilterInterfaces != null)
            {
                targetInterface = contextualFilterInterfaces.FirstOrDefault(i => i.GetGenericArguments()[0].IsAssignableFrom(appContextType));
            }

            return targetInterface;
        }

        private static Func<object, FeatureFilterEvaluationContext, object, Task<bool>> TypeAgnosticEvaluate(Type filterType, MethodInfo method)
        {
            //
            // Get the generic version of the evaluation helper method
            MethodInfo genericHelper = typeof(ContextualFeatureFilterEvaluator).GetMethod(nameof(GenericTypeAgnosticEvaluate),
                BindingFlags.Static | BindingFlags.NonPublic);

            //
            // Create a type specific version of the evaluation helper method
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod
                (filterType, method.GetParameters()[0].ParameterType, method.GetParameters()[1].ParameterType, method.ReturnType);

            //
            // Invoke the method to get the func
            object typeAgnosticDelegate = constructedHelper.Invoke(null, new object[] { method });

            return (Func<object, FeatureFilterEvaluationContext, object, Task<bool>>)typeAgnosticDelegate;
        }

        private static Func<object, FeatureFilterEvaluationContext, object, Task<bool>> GenericTypeAgnosticEvaluate<TTarget, TParam1, TParam2, TReturn>(MethodInfo method)
        {
            Func<TTarget, FeatureFilterEvaluationContext, TParam2, Task<bool>> func = (Func<TTarget, FeatureFilterEvaluationContext, TParam2, Task<bool>>)Delegate.CreateDelegate
                (typeof(Func<TTarget, FeatureFilterEvaluationContext, TParam2, Task<bool>>), method);

            Func<object, FeatureFilterEvaluationContext, object, Task<bool>> genericDelegate = (object target, FeatureFilterEvaluationContext param1, object param2) => func((TTarget)target, param1, (TParam2)param2);

            return genericDelegate;
        }
    }
}
