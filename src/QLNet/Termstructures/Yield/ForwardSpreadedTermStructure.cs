﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    //! Term structure with added spread on the instantaneous forward rate
    /*! \note This term structure will remain linked to the original structure, i.e., any changes in the latter will be
              reflected in this structure as well.

        \ingroup yieldtermstructures

        \test
        - the correctness of the returned values is tested by checking them against numerical calculations.
        - observability against changes in the underlying term structure and in the added spread is checked.
    */
    [PublicAPI]
    public class ForwardSpreadedTermStructure : ForwardRateStructure
    {
        private Handle<YieldTermStructure> originalCurve_;
        private Handle<Quote> spread_;

        public ForwardSpreadedTermStructure(Handle<YieldTermStructure> h, Handle<Quote> spread)
        {
            originalCurve_ = h;
            spread_ = spread;

            originalCurve_.registerWith(update);
            spread_.registerWith(update);
        }

        public override Calendar calendar() => originalCurve_.link.calendar();

        public override DayCounter dayCounter() => originalCurve_.link.dayCounter();

        public override Date maxDate() => originalCurve_.link.maxDate();

        public override double maxTime() => originalCurve_.link.maxTime();

        public override Date referenceDate() => originalCurve_.link.referenceDate();

        public override int settlementDays() => originalCurve_.link.settlementDays();

        //! returns the spreaded forward rate
        protected override double forwardImpl(double t) =>
            originalCurve_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency, true).rate()
            + spread_.link.value();

        //! returns the spreaded zero yield rate
        /* This method must disappear should the spread become a curve */
        protected override double zeroYieldImpl(double t) =>
            originalCurve_.link.zeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true).rate()
            + spread_.link.value();
    }
}
