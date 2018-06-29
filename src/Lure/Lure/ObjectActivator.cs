using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lure
{
    // Source: https://rogerjohansson.blog/2008/02/28/linq-expressions-creating-objects/

    public delegate TObject ObjectActivator<TObject>();

    public delegate TObject ParameterizedObjectActivator<TObject>(params object[] args);

    public delegate TObject ParameterizedObjectActivator<TArg, TObject>(TArg arg);

    public delegate TObject ParameterizedObjectActivator<TArg1, TArg2, TObject>(TArg1 arg1, TArg2 arg2);

    public delegate TObject ParameterizedObjectActivator<TArg1, TArg2, TArg3, TObject>(TArg1 arg1, TArg2 arg2, TArg3 arg3);

    public delegate TObject ParameterizedObjectActivator<TArg1, TArg2, TArg3, TArg4, TObject>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);


    public static class ObjectActivatorFactory
    {
        public static ObjectActivator<TObject> Create<TObject>()
        {
            return Create<TObject>(typeof(TObject));
        }

        public static ObjectActivator<TObject> Create<TObject>(Type objectType)
        {
            var ctor = objectType.GetConstructors().Where(x => x.GetParameters().Length == 0).Single();
            return CreateCore<TObject>(ctor);
        }


        public static ParameterizedObjectActivator<TObject> CreateParameterized<TObject>(params Type[] parameterTypes)
        {
            var ctor = typeof(TObject).GetConstructor(parameterTypes);
            return CreateParameterizedParamsCore<ParameterizedObjectActivator<TObject>, TObject>(ctor);
        }

        public static ParameterizedObjectActivator<TArg, TObject> CreateParameterized<TArg, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg) });
            return CreateParameterizedCore<ParameterizedObjectActivator<TArg, TObject>, TObject>(ctor);
        }

        public static ParameterizedObjectActivator<TArg1, TArg2, TObject> CreateParameterized<TArg1, TArg2, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2) });
            return CreateParameterizedCore<ParameterizedObjectActivator<TArg1, TArg2, TObject>, TObject>(ctor);
        }

        public static ParameterizedObjectActivator<TArg1, TArg2, TArg3, TObject> CreateParameterized<TArg1, TArg2, TArg3, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) });
            return CreateParameterizedCore<ParameterizedObjectActivator<TArg1, TArg2, TArg3, TObject>, TObject>(ctor);
        }

        public static ParameterizedObjectActivator<TArg1, TArg2, TArg3, TArg4, TObject> CreateParameterized<TArg1, TArg2, TArg3, TArg4, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) });
            return CreateParameterizedCore<ParameterizedObjectActivator<TArg1, TArg2, TArg3, TArg4, TObject>, TObject>(ctor);
        }


        private static ObjectActivator<TObject> CreateCore<TObject>(ConstructorInfo ctor)
        {
            if (!typeof(TObject).IsAssignableFrom(ctor.DeclaringType))
            {
                throw new InvalidOperationException("Invalid constructor's declaring type.");
            }

            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda(typeof(ObjectActivator<TObject>), newExpr);
            var compiledLambda = (ObjectActivator<TObject>)lambda.Compile();

            return compiledLambda;
        }

        private static TActivator CreateParameterizedParamsCore<TActivator, TObject>(ConstructorInfo ctor) where TActivator : Delegate
        {
            if (!typeof(TObject).IsAssignableFrom(ctor.DeclaringType))
            {
                throw new InvalidOperationException("Invalid constructor's declaring type.");
            }

            var activatorParameter = Expression.Parameter(typeof(object[]), "args");

            var ctorParams = ctor.GetParameters();
            var ctorParamExprs = new Expression[ctorParams.Length];

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var ctorParamAccessorExpr = Expression.ArrayIndex(activatorParameter, Expression.Constant(i));
                var ctorParamCastExpr = Expression.Convert(ctorParamAccessorExpr, ctorParams[i].ParameterType);
                ctorParamExprs[i] = ctorParamCastExpr;
            }

            var newExpr = Expression.New(ctor, ctorParamExprs);
            var lambda = Expression.Lambda(typeof(TActivator), newExpr, activatorParameter);
            var compiledLambda = (TActivator)lambda.Compile();

            return compiledLambda;
        }

        private static TActivator CreateParameterizedCore<TActivator, TObject>(ConstructorInfo ctor) where TActivator : Delegate
        {
            if (!typeof(TObject).IsAssignableFrom(ctor.DeclaringType))
            {
                throw new InvalidOperationException("Invalid constructor's declaring type.");
            }

            var ctorParams = ctor.GetParameters();
            var ctorParamExprs = ctorParams.Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();

            var newExpr = Expression.New(ctor, ctorParamExprs);
            var lambda = Expression.Lambda(typeof(TActivator), newExpr, ctorParamExprs);
            var compiledLambda = (TActivator)lambda.Compile();

            return compiledLambda;
        }
    }
}
