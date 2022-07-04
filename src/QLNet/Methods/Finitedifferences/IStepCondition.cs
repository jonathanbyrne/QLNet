using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public interface IStepCondition<array_type> where array_type : Vector
    {
        void applyTo(object o, double t);
    }
}