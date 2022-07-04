using QLNet.Methods.montecarlo;

namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public interface IRNGTraits
    {
        ulong nextInt32();
        Sample<double> next();

        IRNGTraits factory(ulong seed);
    }
}