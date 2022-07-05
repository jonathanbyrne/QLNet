﻿using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Pricingengines.swaption;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class ImpliedVolHelper_ : ISolver1d
    {
        private Handle<YieldTermStructure> discountCurve_;
        private IPricingEngine engine_;
        private Instrument.Results results_;
        private double targetValue_;
        private SimpleQuote vol_;

        public ImpliedVolHelper_(Swaption swaption,
            Handle<YieldTermStructure> discountCurve,
            double targetValue,
            double? displacement = 0.0,
            VolatilityType type = VolatilityType.ShiftedLognormal)
        {
            discountCurve_ = discountCurve;
            targetValue_ = targetValue;
            // set an implausible value, so that calculation is forced
            // at first ImpliedVolHelper::operator()(Volatility x) call
            vol_ = new SimpleQuote(-1.0);
            var h = new Handle<Quote>(vol_);
            switch (type)
            {
                case VolatilityType.ShiftedLognormal:
                    engine_ = new BlackSwaptionEngine(discountCurve_, h, new Actual365Fixed(), displacement);
                    break;
                case VolatilityType.Normal:
                    engine_ = new BachelierSwaptionEngine(discountCurve_, h, new Actual365Fixed());
                    break;
                default:
                    Utils.QL_FAIL("unknown VolatilityType (" + type + ")");
                    break;
            }

            swaption.setupArguments(engine_.getArguments());
            results_ = engine_.getResults() as Instrument.Results;
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

        public override double value(double x)
        {
            if (x.IsNotEqual(vol_.value()))
            {
                vol_.setValue(x);
                engine_.calculate();
            }

            return results_.value.Value - targetValue_;
        }
    }
}
