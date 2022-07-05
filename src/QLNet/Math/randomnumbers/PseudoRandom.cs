using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.randomnumbers
{
    [PublicAPI]
    public class PseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativeNormal>
    {
    }
}
