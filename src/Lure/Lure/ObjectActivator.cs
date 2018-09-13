using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lure
{
    public static class ObjectActivatorFactory
    {
        public static Func<TObject> Create<TObject>()
        {
            return Create<TObject>(typeof(TObject));
        }

        public static Func<TObject> Create<TObject>(Type objectType)
        {
            var ctor = objectType.GetConstructors().Single(x => x.GetParameters().Length == 0);
            return CreateCore<TObject>(ctor);
        }


        public static Func<TObject> CreateWithValues<TArg, TObject>(TArg arg)
        {
            var parameterizedActivator = CreateParameterized<TArg, TObject>();
            return () => parameterizedActivator(arg);
        }

        public static Func<TObject> CreateWithValues<TArg1, TArg2, TObject>(TArg1 arg1, TArg2 arg2)
        {
            var parameterizedActivator = CreateParameterized<TArg1, TArg2, TObject>();
            return () => parameterizedActivator(arg1, arg2);
        }

        public static Func<TObject> CreateWithValues<TArg1, TArg2, TArg3, TObject>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var parameterizedActivator = CreateParameterized<TArg1, TArg2, TArg3, TObject>();
            return () => parameterizedActivator(arg1, arg2, arg3);
        }

        public static Func<TObject> CreateWithValues<TArg1, TArg2, TArg3, TArg4, TObject>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var parameterizedActivator = CreateParameterized<TArg1, TArg2, TArg3, TArg4, TObject>();
            return () => parameterizedActivator(arg1, arg2, arg3, arg4);
        }


        public static Func<TObject> CreateParameterized<TObject>(params Type[] parameterTypes)
        {
            var ctor = typeof(TObject).GetConstructor(parameterTypes);
            return CreateParameterizedParamsCore<Func<TObject>, TObject>(ctor);
        }

        public static Func<TArg, TObject> CreateParameterized<TArg, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg) });
            return CreateParameterizedCore<Func<TArg, TObject>, TObject>(ctor);
        }

        public static Func<TArg1, TArg2, TObject> CreateParameterized<TArg1, TArg2, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TObject>, TObject>(ctor);
        }

        public static Func<TArg1, TArg2, TArg3, TObject> CreateParameterized<TArg1, TArg2, TArg3, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TArg3, TObject>, TObject>(ctor);
        }

        public static Func<TArg1, TArg2, TArg3, TArg4, TObject> CreateParameterized<TArg1, TArg2, TArg3, TArg4, TObject>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TArg3, TArg4, TObject>, TObject>(ctor);
        }


        public static Func<TArg, TAs> CreateParameterizedAs<TArg, TObject, TAs>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg) });
            return CreateParameterizedCore<Func<TArg, TAs>, TAs>(ctor);
        }

        public static Func<TArg1, TArg2, TAs> CreateParameterizedAs<TArg1, TArg2, TObject, TAs>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TAs>, TAs>(ctor);
        }

        public static Func<TArg1, TArg2, TArg3, TAs> CreateParameterizedAs<TArg1, TArg2, TArg3, TObject, TAs>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TArg3, TAs>, TAs>(ctor);
        }

        public static Func<TArg1, TArg2, TArg3, TArg4, TAs> CreateParameterizedAs<TArg1, TArg2, TArg3, TArg4, TObject, TAs>()
        {
            var ctor = typeof(TObject).GetConstructor(new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) });
            return CreateParameterizedCore<Func<TArg1, TArg2, TArg3, TArg4, TAs>, TAs>(ctor);
        }


        private static Func<TObject> CreateCore<TObject>(ConstructorInfo ctor)
        {
            if (!typeof(TObject).IsAssignableFrom(ctor.DeclaringType))
            {
                throw new InvalidOperationException("Invalid constructor's declaring type.");
            }

            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda(typeof(Func<TObject>), newExpr);
            var compiledLambda = (Func<TObject>)lambda.Compile();

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
