using QLNet.Math.Distributions;

namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public class PoissonPseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativePoisson> { }
}