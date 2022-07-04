using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;

namespace QLNet.Pricingengines.swaption
{
    [JetBrains.Annotations.PublicAPI] public class BachelierSwaptionEngine : BlackStyleSwaptionEngine<BachelierSpec>
    {
        public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            double vol, DayCounter dc = null,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, dc, 0.0, model)
        { }

        public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<Quote> vol, DayCounter dc = null,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, dc, 0.0, model)
        { }

        public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<SwaptionVolatilityStructure> vol,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, 0.0, model)
        {
            Utils.QL_REQUIRE(vol.link.volatilityType() == VolatilityType.Normal,
                () => "BachelierSwaptionEngine requires normal input volatility");
        }
    }
}