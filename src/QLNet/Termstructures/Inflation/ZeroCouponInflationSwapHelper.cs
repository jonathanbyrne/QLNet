/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Pricingengines.Swap;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    //! Zero-coupon inflation-swap bootstrap helper
    [PublicAPI]
    public class ZeroCouponInflationSwapHelper : BootstrapHelper<ZeroInflationTermStructure>
    {
        protected Calendar calendar_;
        protected DayCounter dayCounter_;
        protected Date maturity_;
        protected BusinessDayConvention paymentConvention_;
        protected Period swapObsLag_;
        protected ZeroCouponInflationSwap zciis_;
        protected ZeroInflationIndex zii_;

        public ZeroCouponInflationSwapHelper(
            Handle<Quote> quote,
            Period swapObsLag, // lag on swap observation of index
            Date maturity,
            Calendar calendar, // index may have null calendar as valid on every day
            BusinessDayConvention paymentConvention,
            DayCounter dayCounter,
            ZeroInflationIndex zii)
            : base(quote)
        {
            swapObsLag_ = swapObsLag;
            maturity_ = maturity;
            calendar_ = calendar;
            paymentConvention_ = paymentConvention;
            dayCounter_ = dayCounter;
            zii_ = zii;

            if (zii_.interpolated())
            {
                // if interpolated then simple
                earliestDate_ = maturity_ - swapObsLag_;
                latestDate_ = maturity_ - swapObsLag_;
            }
            else
            {
                // but if NOT interpolated then the value is valid
                // for every day in an inflation period so you actually
                // get an extended validity, however for curve building
                // just put the first date because using that convention
                // for the base date throughout
                var limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                    zii_.frequency());
                earliestDate_ = limStart.Key;
                latestDate_ = limStart.Key;
            }

            // check that the observation lag of the swap
            // is compatible with the availability lag of the index AND
            // it's interpolation (assuming the start day is spot)
            if (zii_.interpolated())
            {
                var pShift = new Period(zii_.frequency());
                Utils.QL_REQUIRE(swapObsLag_ - pShift > zii_.availabilityLag(), () =>
                    "inconsistency between swap observation of index "
                    + swapObsLag_ +
                    " index availability " + zii_.availabilityLag() +
                    " index period " + pShift +
                    " and index availability " + zii_.availabilityLag() +
                    " need (obsLag-index period) > availLag");
            }

            Settings.registerWith(update);
        }

        public override double impliedQuote()
        {
            // what does the term structure imply?
            // in this case just the same value ... trivial case
            // (would not be so for an inflation-linked bond)
            zciis_.recalculate();
            return zciis_.fairRate();
        }

        public override void setTermStructure(ZeroInflationTermStructure z)
        {
            base.setTermStructure(z);

            // set up a new ZCIIS
            // but this one does NOT own its inflation term structure
            var own = false;
            var K = quote().link.value();

            // The effect of the new inflation term structure is
            // felt via the effect on the inflation index
            var zits = new Handle<ZeroInflationTermStructure>(z, own);

            var new_zii = zii_.clone(zits);

            var nominal = 1000000.0; // has to be something but doesn't matter what
            var start = z.nominalTermStructure().link.referenceDate();
            zciis_ = new ZeroCouponInflationSwap(
                ZeroCouponInflationSwap.Type.Payer,
                nominal, start, maturity_,
                calendar_, paymentConvention_, dayCounter_, K, // fixed side & fixed rate
                new_zii, swapObsLag_);
            // Because very simple instrument only takes
            // standard discounting swap engine.
            zciis_.setPricingEngine(new DiscountingSwapEngine(z.nominalTermStructure()));
        }
    }

    //! Year-on-year inflation-swap bootstrap helper
}
