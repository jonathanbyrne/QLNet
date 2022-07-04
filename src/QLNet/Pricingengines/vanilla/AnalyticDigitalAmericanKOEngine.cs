using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class AnalyticDigitalAmericanKOEngine : AnalyticDigitalAmericanEngine
    {
        public AnalyticDigitalAmericanKOEngine(GeneralizedBlackScholesProcess engine) :
            base(engine)
        { }

        public override bool knock_in() => false;
    }
}