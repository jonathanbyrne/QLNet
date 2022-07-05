using JetBrains.Annotations;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class MakeMCEuropeanEngine<RNG> : MakeMCEuropeanEngine<RNG, Statistics> where RNG : IRSG, new()
    {
        public MakeMCEuropeanEngine(GeneralizedBlackScholesProcess process) : base(process)
        {
        }
    }
}
