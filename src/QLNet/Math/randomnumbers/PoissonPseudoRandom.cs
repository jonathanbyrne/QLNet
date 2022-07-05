using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.randomnumbers
{
    [PublicAPI]
    public class PoissonPseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativePoisson>
    {
    }
}
