using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public class NullCondition<array_type> : IStepCondition<array_type> where array_type : Vector
    {
        public void applyTo(object a, double t)
        {
            // Nothing to do here
        }
    }
}