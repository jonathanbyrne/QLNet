﻿using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Quotes;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class PriceError : ISolver1d
    {
        private IPricingEngine engine_;
        private Instrument.Results results_;
        private double targetValue_;
        private SimpleQuote vol_;

        public PriceError(IPricingEngine engine, SimpleQuote vol, double targetValue)
        {
            engine_ = engine;
            vol_ = vol;
            targetValue_ = targetValue;

            results_ = engine_.getResults() as Instrument.Results;
            QLNet.Utils.QL_REQUIRE(results_ != null, () => "pricing engine does not supply needed results");
        }

        public override double value(double x)
        {
            vol_.setValue(x);
            engine_.calculate();
            return results_.value.GetValueOrDefault() - targetValue_;
        }
    }
}
