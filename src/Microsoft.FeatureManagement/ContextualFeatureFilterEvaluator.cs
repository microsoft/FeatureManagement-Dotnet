// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a performance efficient method of evaluating IContextualFeatureFilter&lt;T&gt; without knowing what the generic type parameter is.
    /// </summary>
    class ContextualFeatureFilterEvaluator : IContextualFeatureFilter<IFeatureFilterContext>
    {
        private IContextualFeatureFilter _filter;
        private Func<object, FeatureFilterEvaluationContext, object, bool> _evaluateFunc;

        public ContextualFeatureFilterEvaluator(IContextualFeatureFilter filter, Type appContextType)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            //
            // Extract IContextualFeatureFilter<T>.Evaluate method.
            IEnumerable<Type> contextualFilterInterfaces = filter.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(IContextualFeatureFilter<>)));

            Type targetInterface = contextualFilterInterfaces.FirstOrDefault(i => i.GetGenericArguments()[0].IsAssignableFrom(appContextType));

            if (targetInterface != null)
            {
                MethodInfo evaluateMethod = targetInterface.GetMethod(nameof(IContextualFeatureFilter<IFeatureFilterContext>.Evaluate), BindingFlags.Public | BindingFlags.Instance);

                _evaluateFunc = TypeAgnosticEvaluate(filter.GetType(), evaluateMethod);
            }

            _filter = filter;
        }

        public bool Evaluate(FeatureFilterEvaluationContext evaluationContext, IFeatureFilterContext context)
        {
            if (_evaluateFunc == null)
            {
                return false;
            }

            return _evaluateFunc(_filter, evaluationContext, context);
        }

        static Func<object, FeatureFilterEvaluationContext, object, bool> TypeAgnosticEvaluate(Type filterType, MethodInfo method)
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
            object ret = constructedHelper.Invoke(null, new object[] { method });

            return (Func<object, FeatureFilterEvaluationContext, object, bool>)ret;
        }

        static Func<object, FeatureFilterEvaluationContext, object, bool> GenericTypeAgnosticEvaluate<TTarget, TParam1, TParam2, TReturn>(MethodInfo method)
        {
            Func<TTarget, FeatureFilterEvaluationContext, TParam2, bool> func = (Func<TTarget, FeatureFilterEvaluationContext, TParam2, bool>)Delegate.CreateDelegate
                (typeof(Func<TTarget, FeatureFilterEvaluationContext, TParam2, bool>), method);

            Func<object, FeatureFilterEvaluationContext, object, bool> ret = (object target, FeatureFilterEvaluationContext param1, object param2) => func((TTarget)target, param1, (TParam2)param2);

            return ret;
        }

        public bool Evaluate(FeatureFilterEvaluationContext context) => false;
    }
}
