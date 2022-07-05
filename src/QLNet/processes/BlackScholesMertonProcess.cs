using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.Processes
{
    [PublicAPI]
    public class BlackScholesMertonProcess : GeneralizedBlackScholesProcess
    {
        public BlackScholesMertonProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS)
            : this(x0, dividendTS, riskFreeTS, blackVolTS, new EulerDiscretization())
        {
        }

        public BlackScholesMertonProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IDiscretization1D d)
            : base(x0, dividendTS, riskFreeTS, blackVolTS, d)
        {
        }
    }
}
