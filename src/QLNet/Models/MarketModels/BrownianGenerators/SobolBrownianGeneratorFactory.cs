namespace QLNet.Models.MarketModels.BrownianGenerators
{
    [JetBrains.Annotations.PublicAPI] public class SobolBrownianGeneratorFactory : IBrownianGeneratorFactory
    {
        public SobolBrownianGeneratorFactory(SobolBrownianGenerator.Ordering ordering, ulong seed = 0,
            SobolRsg.DirectionIntegers integers = SobolRsg.DirectionIntegers.Jaeckel)
        {
            ordering_ = ordering;
            seed_ = seed;
            integers_ = integers;
        }

        public IBrownianGenerator create(int factors, int steps) => new SobolBrownianGenerator(factors, steps, ordering_, seed_, integers_);

        private SobolBrownianGenerator.Ordering ordering_;
        private ulong seed_;
        private SobolRsg.DirectionIntegers integers_;
    }
}