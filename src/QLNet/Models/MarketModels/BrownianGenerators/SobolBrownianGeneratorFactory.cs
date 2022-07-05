using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;

namespace QLNet.Models.MarketModels.BrownianGenerators
{
    [PublicAPI]
    public class SobolBrownianGeneratorFactory : IBrownianGeneratorFactory
    {
        private SobolRsg.DirectionIntegers integers_;
        private SobolBrownianGenerator.Ordering ordering_;
        private ulong seed_;

        public SobolBrownianGeneratorFactory(SobolBrownianGenerator.Ordering ordering, ulong seed = 0,
            SobolRsg.DirectionIntegers integers = SobolRsg.DirectionIntegers.Jaeckel)
        {
            ordering_ = ordering;
            seed_ = seed;
            integers_ = integers;
        }

        public IBrownianGenerator create(int factors, int steps) => new SobolBrownianGenerator(factors, steps, ordering_, seed_, integers_);
    }
}
