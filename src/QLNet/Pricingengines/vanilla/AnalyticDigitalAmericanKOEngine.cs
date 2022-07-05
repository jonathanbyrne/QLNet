using JetBrains.Annotations;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class AnalyticDigitalAmericanKOEngine : AnalyticDigitalAmericanEngine
    {
        public AnalyticDigitalAmericanKOEngine(GeneralizedBlackScholesProcess engine) :
            base(engine)
        {
        }

        public override bool knock_in() => false;
    }
}
