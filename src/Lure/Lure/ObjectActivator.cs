using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lure
{
    // Source: https://rogerjohansson.blog/2008/02/28/linq-expressions-creating-objects/

    public delegate T ObjectActivator<T>(params object[] args);

    public static class ObjectActivatorFactory
    {
        public static ObjectActivator<T> CreateDefault<T>()
        {
            return CreateDefault<T>(typeof(T));
        }

        public static ObjectActivator<T> CreateDefault<T>(Type type)
        {
            var ctor = type.GetConstructors().Where(x => x.GetParameters().Length == 0).First();
            return Create<T>(ctor);
        }

        public static ObjectActivator<T> Create<T>(params Type[] types)
        {
            var ctor = typeof(T).GetConstructor(types);
            return Create<T>(ctor);
        }

        public static ObjectActivator<T> Create<T>(ConstructorInfo ctor)
        {
            if (!typeof(T).IsAssignableFrom(ctor.DeclaringType))
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
            var lambda = Expression.Lambda(typeof(ObjectActivator<T>), newExpr, activatorParameter);
            var compiledLambda = (ObjectActivator<T>)lambda.Compile();

            return compiledLambda;
        }
    }
}
