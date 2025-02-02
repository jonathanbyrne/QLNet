﻿using JetBrains.Annotations;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class UnitDisplacedBlackYoYInflationCouponPricer : YoYInflationCouponPricer
    {
        public UnitDisplacedBlackYoYInflationCouponPricer(Handle<YoYOptionletVolatilitySurface> capletVol = null)
            : base(capletVol)
        {
        }

        protected override double optionletPriceImp(Option.Type optionType, double effStrike,
            double forward, double stdDev) =>
            PricingEngines.Utils.blackFormula(optionType,
                effStrike + 1.0,
                forward + 1.0,
                stdDev);
    }
}
