using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lure
{
    // TODO: Refactor ObjectActivatorFactory class
    // https://rogerjohansson.blog/2008/02/28/linq-expressions-creating-objects/

    public delegate T ObjectActivator<T>(params object[] args);

    public static class ObjectActivatorFactory
    {
        public static ObjectActivator<T> Create<T>()
        {
            return Create<T>(typeof(T));
        }

        public static ObjectActivator<T> Create<T>(Type type)
        {
            var ctor = type.GetConstructors().Where(x => x.GetParameters().Length == 0).First();
            return Create<T>(ctor);
        }

        public static ObjectActivator<T> Create<T>(ConstructorInfo ctor)
        {
            var type = ctor.DeclaringType;
            var parameters = ctor.GetParameters();

            ParameterExpression param = Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp = new Expression[parameters.Length];

            //pick each arg from the params array
            //and create a typed expression of them
            for (int i = 0; i < parameters.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = parameters[i].ParameterType;

                Expression paramAccessorExp = Expression.ArrayIndex(param, index);

                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            LambdaExpression lambda = Expression.Lambda(typeof(ObjectActivator<T>), newExp, param);

            //compile it
            ObjectActivator<T> compiled = (ObjectActivator<T>)lambda.Compile();
            return compiled;
        }
    }
}
