using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class MakeMCAmericanEngine<RNG> : MakeMCAmericanEngine<RNG, Statistics>
        where RNG : IRSG, new()
    {
        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process) : base(process) { }
    }
}