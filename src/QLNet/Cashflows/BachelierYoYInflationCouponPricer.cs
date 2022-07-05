using JetBrains.Annotations;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class BachelierYoYInflationCouponPricer : YoYInflationCouponPricer
    {
        public BachelierYoYInflationCouponPricer(Handle<YoYOptionletVolatilitySurface> capletVol = null)
            : base(capletVol)
        {
        }

        protected override double optionletPriceImp(Option.Type optionType, double effStrike,
            double forward, double stdDev) =>
            Utils.bachelierBlackFormula(optionType,
                effStrike,
                forward,
                stdDev);
    }
}
