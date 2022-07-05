using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public interface IStepCondition<array_type> where array_type : Vector
    {
        void applyTo(object o, double t);
    }
}
