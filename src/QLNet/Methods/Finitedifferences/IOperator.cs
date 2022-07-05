using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public interface IOperator : ICloneable
    {
        IOperator add
            (IOperator A, IOperator B);

        Vector applyTo(Vector v);

        IOperator identity(int size);

        bool isTimeDependent();

        IOperator multiply(double a, IOperator D);

        void setTime(double t);

        int size();

        Vector solveFor(Vector rhs);

        IOperator subtract(IOperator A, IOperator B);
    }
}
