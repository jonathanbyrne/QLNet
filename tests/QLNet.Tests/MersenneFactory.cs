using QLNet.Math.randomnumbers;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public class MersenneFactory : IRNGFactory
{
    //typedef RandomSequenceGenerator<MersenneTwisterUniformRng> MersenneTwisterUniformRsg;
    //typedef MersenneTwisterUniformRsg generator_type;
    public IRNG make(int dim, ulong seed) => new RandomSequenceGenerator<MersenneTwisterUniformRng>(dim, seed);

    public string name() => "Mersenne Twister";
}