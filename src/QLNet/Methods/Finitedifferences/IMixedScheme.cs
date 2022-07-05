using JetBrains.Annotations;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public interface IMixedScheme
    {
        void setStep(double dt);

        void step(ref object a, double t, double theta = 1.0);
    }
}
