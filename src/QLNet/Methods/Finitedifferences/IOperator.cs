using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public interface IOperator : ICloneable
    {
        int size();
        IOperator identity(int size);
        Vector applyTo(Vector v);
        Vector solveFor(Vector rhs);

        IOperator multiply(double a, IOperator D);
        IOperator add
            (IOperator A, IOperator B);
        IOperator subtract(IOperator A, IOperator B);

        bool isTimeDependent();
        void setTime(double t);
    }
}