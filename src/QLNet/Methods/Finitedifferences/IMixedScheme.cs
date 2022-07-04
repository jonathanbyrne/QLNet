namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public interface IMixedScheme
    {
        void step(ref object a, double t, double theta = 1.0);
        void setStep(double dt);
    }
}