using JetBrains.Annotations;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class BlackYoYInflationCouponPricer : YoYInflationCouponPricer
    {
        public BlackYoYInflationCouponPricer(Handle<YoYOptionletVolatilitySurface> capletVol)
            : base(capletVol)
        {
        }

        protected override double optionletPriceImp(Option.Type optionType, double effStrike,
            double forward, double stdDev) =>
            PricingEngines.Utils.blackFormula(optionType,
                effStrike,
                forward,
                stdDev);
    }
}
