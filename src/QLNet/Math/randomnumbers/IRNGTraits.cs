using JetBrains.Annotations;
using QLNet.Methods.montecarlo;

namespace QLNet.Math.randomnumbers
{
    [PublicAPI]
    public interface IRNGTraits
    {
        IRNGTraits factory(ulong seed);

        Sample<double> next();

        ulong nextInt32();
    }
}
