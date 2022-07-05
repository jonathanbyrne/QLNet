using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Bond;

namespace QLNet.Pricingengines.Bond
{
    [PublicAPI]
    public class BlackCallableZeroCouponBondEngine : BlackCallableFixedRateBondEngine
    {
        //! volatility is the quoted fwd yield volatility, not price vol
        public BlackCallableZeroCouponBondEngine(Handle<Quote> fwdYieldVol, Handle<YieldTermStructure> discountCurve)
            : base(fwdYieldVol, discountCurve)
        {
        }

        //! volatility is the quoted fwd yield volatility, not price vol
        public BlackCallableZeroCouponBondEngine(Handle<CallableBondVolatilityStructure> yieldVolStructure,
            Handle<YieldTermStructure> discountCurve)
            : base(yieldVolStructure, discountCurve)
        {
        }
    }
}
