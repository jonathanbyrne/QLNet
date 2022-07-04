using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace QLNet.Patterns
{
    public static class DynamicModuleLambdaCompiler
    {
        public static Func<T> GenerateFactory<T>() where T : new()
        {
            Expression<Func<T>> expr = () => new T();
            var newExpr = (NewExpression)expr.Body;

#if NET452
         var method = new DynamicMethod(
            name: "lambda",
            returnType: newExpr.Type,
            parameterTypes: new Type[0],
            m: typeof(DynamicModuleLambdaCompiler).Module,
            skipVisibility: true);
#else
            var method = new DynamicMethod(
                name: "lambda",
                returnType: newExpr.Type,
                parameterTypes: new Type[0],
                m: typeof(DynamicModuleLambdaCompiler).GetTypeInfo().Module,
                skipVisibility: true);
#endif

            var ilGen = method.GetILGenerator();
            // Constructor for value types could be null
            if (newExpr.Constructor != null)
            {
                ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
            }
            else
            {
                var temp = ilGen.DeclareLocal(newExpr.Type);
                ilGen.Emit(OpCodes.Ldloca, temp);
                ilGen.Emit(OpCodes.Initobj, newExpr.Type);
                ilGen.Emit(OpCodes.Ldloc, temp);
            }

            ilGen.Emit(OpCodes.Ret);

            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }
    }
}