using System.Linq;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Pricingengines.CapFloor;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class ImpliedVolHelper : ISolver1d
    {
        private IPricingEngine engine_;
        private Handle<YieldTermStructure> discountCurve_;
        private double targetValue_;
        private SimpleQuote vol_;
        private Instrument.Results results_;

        public ImpliedVolHelper(CapFloor cap,
            Handle<YieldTermStructure> discountCurve,
            double targetValue,
            double displacement,
            VolatilityType type)
        {
            discountCurve_ = discountCurve;
            targetValue_ = targetValue;

            vol_ = new SimpleQuote(-1.0);
            var h = new Handle<Quote>(vol_);

            switch (type)
            {
                case VolatilityType.ShiftedLognormal:
                    engine_ = new BlackCapFloorEngine(discountCurve_, h, new Actual365Fixed(), displacement);
                    break;

                case VolatilityType.Normal:
                    engine_ = new BachelierCapFloorEngine(discountCurve_, h, new Actual365Fixed());
                    break;

                default:
                    Utils.QL_FAIL("unknown VolatilityType (" + type.ToString() + ")");
                    break;
            }

            cap.setupArguments(engine_.getArguments());
            results_ = engine_.getResults() as Instrument.Results;

        }

        public override double value(double x)
        {
            if (x.IsNotEqual(vol_.value()))
            {
                vol_.setValue(x);
                engine_.calculate();
            }

            return results_.value.Value - targetValue_;
        }

        public override double derivative(double x)
        {
            if (x.IsNotEqual(vol_.value()))
            {
                vol_.setValue(x);
                engine_.calculate();
            }
            Utils.QL_REQUIRE(results_.additionalResults.Keys.Contains("vega"), () => "vega not provided");
            return (double)results_.additionalResults["vega"];
        }
    }
}