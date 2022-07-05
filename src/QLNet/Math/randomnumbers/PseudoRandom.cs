using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.RandomNumbers
{
    [PublicAPI]
    public class PseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativeNormal>
    {
    }
}
