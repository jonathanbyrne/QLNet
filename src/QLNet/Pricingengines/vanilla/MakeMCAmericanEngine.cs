using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class MakeMCAmericanEngine<RNG> : MakeMCAmericanEngine<RNG, Statistics>
        where RNG : IRSG, new()
    {
        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process) : base(process)
        {
        }
    }
}
