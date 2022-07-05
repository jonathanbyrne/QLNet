/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.PricingEngines.Swap;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    //! Basis Swap Rate Helper
    /*! Rate helper for bootstrapping over basis swap spreads
        Assumes that you have, at a minimum, either:
        - shortIndex with attached YieldTermStructure
        - longIndex with attached YieldTermStructure
        - Discount curve linked to discount swap engine
        The other leg is then solved for i.e. index curve (if no
        YieldTermStructure is attached to its index).
        The settlement date of the spot is
        assumed to be equal to the settlement date of the swap itself.
                \ingroup termstructures
    */
    [PublicAPI]
    public class BasisSwapHelper : RelativeDateRateHelper
    {
        protected Handle<YieldTermStructure> discountHandle_;
        protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_ = new RelinkableHandle<YieldTermStructure>();
        protected IborIndex longIndex_;
        protected BusinessDayConvention rollConvention_;
        protected Calendar settlementCalendar_;
        protected int settlementDays_;
        protected IborIndex shortIndex_;
        protected bool spreadOnShort_, eom_;
        protected BasisSwap swap_;
        protected Period swapTenor_;
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public BasisSwapHelper(Handle<Quote> spreadQuote,
            int settlementDays,
            Period swapTenor,
            Calendar settlementCalendar,
            BusinessDayConvention rollConvention,
            IborIndex shortIndex,
            IborIndex longIndex,
            Handle<YieldTermStructure> discount = null,
            bool eom = true,
            bool spreadOnShort = true)
            : base(spreadQuote)
        {
            settlementDays_ = settlementDays;
            settlementCalendar_ = settlementCalendar;
            swapTenor_ = swapTenor;
            rollConvention_ = rollConvention;
            shortIndex_ = shortIndex;
            longIndex_ = longIndex;
            spreadOnShort_ = spreadOnShort;
            eom_ = eom;
            discountHandle_ = discount ?? new Handle<YieldTermStructure>();

            var shortIndexHasCurve = !shortIndex_.forwardingTermStructure().empty();
            var longIndexHasCurve = !longIndex_.forwardingTermStructure().empty();

            QLNet.Utils.QL_REQUIRE(!(shortIndexHasCurve && longIndexHasCurve), () => "Have all curves, nothing to solve for.");

            /* Link the curve being bootstrapped to the index if the index has
            no projection curve */
            if (!shortIndexHasCurve)
            {
                shortIndex_ = shortIndex_.clone(termStructureHandle_);
                //shortIndex_.unregisterWith(termStructureHandle_.link.update);
            }
            else if (!longIndexHasCurve)
            {
                longIndex_ = longIndex_.clone(termStructureHandle_);
                //longIndex_.unregisterWith(termStructureHandle_.link.update);
            }
            else
            {
                QLNet.Utils.QL_FAIL("Need one leg of the basis swap to have its forward curve.");
            }

            shortIndex_.registerWith(update);
            longIndex_.registerWith(update);
            discountHandle_.registerWith(update);

            initializeDates();
        }

        //! \name RateHelper interface
        //@{

        public override double impliedQuote()
        {
            QLNet.Utils.QL_REQUIRE(termStructure_ != null, () => "Term structure needs to be set");
            swap_.recalculate();
            return spreadOnShort_ ? swap_.fairShortSpread() : swap_.fairLongSpread();
        }

        public override void setTermStructure(YieldTermStructure t)
        {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
            discountRelinkableHandle_.linkTo(discountHandle_.empty() ? t : discountHandle_, false);
        }

        //@}
        //! \name inspectors
        //@{
        public BasisSwap swap() => swap_;
        //@}

        protected override void initializeDates()
        {
            var settlementDate = settlementCalendar_.advance(evaluationDate_, settlementDays_, TimeUnit.Days);
            var maturityDate = settlementDate + swapTenor_;

            var shortLegTenor = shortIndex_.tenor();
            var shortLegSchedule = new MakeSchedule()
                .from(settlementDate)
                .to(maturityDate)
                .withTenor(shortLegTenor)
                .withCalendar(settlementCalendar_)
                .withConvention(rollConvention_)
                .endOfMonth(eom_)
                .value();

            var longLegTenor = longIndex_.tenor();
            var longLegSchedule = new MakeSchedule()
                .from(settlementDate)
                .to(maturityDate)
                .withTenor(longLegTenor)
                .withCalendar(settlementCalendar_)
                .withConvention(rollConvention_)
                .endOfMonth(eom_)
                .value();

            var nominal = 1.0;
            var shortLegSpread = 0.0;
            var longLegSpread = 0.0;

            if (spreadOnShort_)
            {
                shortLegSpread = quote_.link.value();
            }
            else
            {
                longLegSpread = quote_.link.value();
            }

            /* Arbitrarily set the swap as paying the long index */
            swap_ = new BasisSwap(BasisSwap.Type.Payer, nominal,
                shortLegSchedule, shortIndex_, shortLegSpread, shortIndex_.dayCounter(),
                longLegSchedule, longIndex_, longLegSpread, longIndex_.dayCounter(),
                BusinessDayConvention.Following);

            IPricingEngine engine;

            engine = new DiscountingSwapEngine(discountHandle_, false, settlementDate, settlementDate);
            engine.reset();

            swap_.setPricingEngine(engine);

            earliestDate_ = swap_.startDate();
            latestDate_ = swap_.maturityDate();
            maturityDate_ = latestRelevantDate_ = swap_.maturityDate();
        }
    }
}
