using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class MakeMCEuropeanEngine<RNG> : MakeMCEuropeanEngine<RNG, Statistics> where RNG : IRSG, new()
    {
        public MakeMCEuropeanEngine(GeneralizedBlackScholesProcess process) : base(process)
        {
        }
    }
}
