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
    /// Provides a performance efficient method of evaluating IContextualFeatureVariantAssigner&lt;T&gt; without knowing what the generic type parameter is.
    /// </summary>
    sealed class ContextualFeatureVariantAssignerEvaluator : IContextualFeatureVariantAssigner<object>
    {
        private IFeatureVariantAssignerMetadata _filter;
        private Func<object, FeatureVariantAssignmentContext, object, CancellationToken, ValueTask<FeatureVariant>> _evaluateFunc;

        public ContextualFeatureVariantAssignerEvaluator(IFeatureVariantAssignerMetadata assigner, Type appContextType)
        {
            if (assigner == null)
            {
                throw new ArgumentNullException(nameof(assigner));
            }

            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            Type targetInterface = GetContextualAssignerInterface(assigner, appContextType);

            //
            // Extract IContextualFeatureFilter<T>.EvaluateAsync method.
            if (targetInterface != null)
            {
                MethodInfo evaluateMethod = targetInterface.GetMethod(nameof(IContextualFeatureVariantAssigner<object>.AssignVariantAsync), BindingFlags.Public | BindingFlags.Instance);

                _evaluateFunc = TypeAgnosticEvaluate(assigner.GetType(), evaluateMethod);
            }

            _filter = assigner;
        }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext assignmentContext, object context, CancellationToken cancellationToken)
        {
            if (_evaluateFunc == null)
            {
                return new ValueTask<FeatureVariant>((FeatureVariant)null);
            }

            return _evaluateFunc(_filter, assignmentContext, context, cancellationToken);
        }

        public static bool IsContextualFilter(IFeatureVariantAssignerMetadata assigner, Type appContextType)
        {
            return GetContextualAssignerInterface(assigner, appContextType) != null;
        }

        private static Type GetContextualAssignerInterface(IFeatureVariantAssignerMetadata assigner, Type appContextType)
        {
            IEnumerable<Type> contextualAssignerInterfaces = assigner.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(IContextualFeatureVariantAssigner<>)));

            Type targetInterface = null;

            if (contextualAssignerInterfaces != null)
            {
                targetInterface = contextualAssignerInterfaces.FirstOrDefault(i => i.GetGenericArguments()[0].IsAssignableFrom(appContextType));
            }

            return targetInterface;
        }

        private static Func<object, FeatureVariantAssignmentContext, object, CancellationToken, ValueTask<FeatureVariant>> TypeAgnosticEvaluate(Type filterType, MethodInfo method)
        {
            //
            // Get the generic version of the evaluation helper method
            MethodInfo genericHelper = typeof(ContextualFeatureVariantAssignerEvaluator).GetMethod(nameof(GenericTypeAgnosticEvaluate),
                BindingFlags.Static | BindingFlags.NonPublic);

            //
            // Create a type specific version of the evaluation helper method
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod
                (filterType,
                method.GetParameters()[0].ParameterType,
                method.GetParameters()[1].ParameterType,
                method.GetParameters()[2].ParameterType,
                method.ReturnType);

            //
            // Invoke the method to get the func
            object typeAgnosticDelegate = constructedHelper.Invoke(null, new object[] { method });

            return (Func<object, FeatureVariantAssignmentContext, object, CancellationToken, ValueTask<FeatureVariant>>)typeAgnosticDelegate;
        }

        private static Func<object, FeatureVariantAssignmentContext, object, CancellationToken, ValueTask<FeatureVariant>> GenericTypeAgnosticEvaluate<TTarget, TParam1, TParam2, TParam3, TReturn>(MethodInfo method)
        {
            Func<TTarget, FeatureVariantAssignmentContext, TParam2, CancellationToken, ValueTask<FeatureVariant>> func =
                (Func<TTarget, FeatureVariantAssignmentContext, TParam2, CancellationToken, ValueTask<FeatureVariant>>)
                Delegate.CreateDelegate(typeof(Func<TTarget, FeatureVariantAssignmentContext, TParam2, CancellationToken, ValueTask<FeatureVariant>>), method);

            Func<object, FeatureVariantAssignmentContext, object, CancellationToken, ValueTask<FeatureVariant>> genericDelegate =
                (object target, FeatureVariantAssignmentContext param1, object param2, CancellationToken param3) =>
                    func((TTarget)target, param1, (TParam2)param2, param3);

            return genericDelegate;
        }
    }
}
