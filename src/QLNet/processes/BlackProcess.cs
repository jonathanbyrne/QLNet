using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.Processes
{
    [PublicAPI]
    public class BlackProcess : GeneralizedBlackScholesProcess
    {
        public BlackProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS)
            : this(x0, riskFreeTS, blackVolTS, new EulerDiscretization())
        {
        }

        public BlackProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IDiscretization1D d)
            : base(x0, riskFreeTS, riskFreeTS, blackVolTS, d)
        {
        }
    }
}
