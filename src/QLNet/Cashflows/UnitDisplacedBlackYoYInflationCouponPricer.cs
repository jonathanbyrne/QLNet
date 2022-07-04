using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class UnitDisplacedBlackYoYInflationCouponPricer : YoYInflationCouponPricer
    {
        public UnitDisplacedBlackYoYInflationCouponPricer(Handle<YoYOptionletVolatilitySurface> capletVol = null)
            : base(capletVol)
        { }

        protected override double optionletPriceImp(Option.Type optionType, double effStrike,
            double forward, double stdDev) =>
            Utils.blackFormula(optionType,
                effStrike + 1.0,
                forward + 1.0,
                stdDev);
    }
}