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

using System;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class OISRateHelper : RelativeDateRateHelper
    {
        protected OvernightIndex overnightIndex_;
        protected int settlementDays_;
        protected OvernightIndexedSwap swap_;
        protected Period tenor_;
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public OISRateHelper(int settlementDays,
            Period tenor, // swap maturity
            Handle<Quote> fixedRate,
            OvernightIndex overnightIndex)
            : base(fixedRate)
        {
            settlementDays_ = settlementDays;
            tenor_ = tenor;
            overnightIndex_ = overnightIndex;
            overnightIndex_.registerWith(update);
            initializeDates();
        }

        public override double impliedQuote()
        {
            if (termStructure_ == null)
            {
                throw new ArgumentException("term structure not set");
            }

            // we didn't register as observers - force calculation
            swap_.recalculate();
            return swap_.fairRate().Value;
        }

        public override void setTermStructure(YieldTermStructure t)
        {
            // no need to register---the index is not lazy
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        public OvernightIndexedSwap swap() => swap_;

        protected override void initializeDates()
        {
            // dummy OvernightIndex with curve/swap arguments
            // review here
            IborIndex clonedIborIndex = overnightIndex_.clone(termStructureHandle_);
            var clonedOvernightIndex = clonedIborIndex as OvernightIndex;

            swap_ = new MakeOIS(tenor_, clonedOvernightIndex, 0.0)
                .withSettlementDays(settlementDays_)
                .withDiscountingTermStructure(termStructureHandle_);

            earliestDate_ = swap_.startDate();
            latestDate_ = swap_.maturityDate();
        }
    }

    //! Rate helper for bootstrapping over Overnight Indexed Swap rates
}
