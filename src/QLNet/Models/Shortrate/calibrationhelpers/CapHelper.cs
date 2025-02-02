﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.PricingEngines.CapFloor;
using QLNet.PricingEngines.Swap;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Models.Shortrate.calibrationhelpers
{
    [PublicAPI]
    public class CapHelper : CalibrationHelper
    {
        private Cap cap_;
        private DayCounter fixedLegDayCounter_;
        private Frequency fixedLegFrequency_;
        private bool includeFirstSwaplet_;
        private IborIndex index_;
        private Period length_;

        public CapHelper(Period length,
            Handle<Quote> volatility,
            IborIndex index,
            // data for ATM swap-rate calculation
            Frequency fixedLegFrequency,
            DayCounter fixedLegDayCounter,
            bool includeFirstSwaplet,
            Handle<YieldTermStructure> termStructure,
            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError)
            : base(volatility, termStructure, errorType)
        {
            length_ = length;
            index_ = index;
            fixedLegFrequency_ = fixedLegFrequency;
            fixedLegDayCounter_ = fixedLegDayCounter;
            includeFirstSwaplet_ = includeFirstSwaplet;

            index_.registerWith(update);
        }

        public override void addTimesTo(List<double> times)
        {
            calculate();
            var args = new CapFloor.Arguments();
            cap_.setupArguments(args);
            var capTimes = new DiscretizedCapFloor(args,
                termStructure_.link.referenceDate(),
                termStructure_.link.dayCounter()).mandatoryTimes();
            for (var i = 0; i < capTimes.Count; i++)
            {
                times.Insert(times.Count, capTimes[i]);
            }
        }

        public override double blackPrice(double sigma)
        {
            calculate();
            Quote vol = new SimpleQuote(sigma);
            IPricingEngine black = new BlackCapFloorEngine(termStructure_,
                new Handle<Quote>(vol));
            cap_.setPricingEngine(black);
            var value = cap_.NPV();
            cap_.setPricingEngine(engine_);
            return value;
        }

        public override double modelValue()
        {
            calculate();
            cap_.setPricingEngine(engine_);
            return cap_.NPV();
        }

        protected override void performCalculations()
        {
            var indexTenor = index_.tenor();
            var fixedRate = 0.04; // dummy value
            Date startDate, maturity;
            if (includeFirstSwaplet_)
            {
                startDate = termStructure_.link.referenceDate();
                maturity = termStructure_.link.referenceDate() + length_;
            }
            else
            {
                startDate = termStructure_.link.referenceDate() + indexTenor;
                maturity = termStructure_.link.referenceDate() + length_;
            }

            var dummyIndex = new IborIndex("dummy",
                indexTenor,
                index_.fixingDays(),
                index_.currency(),
                index_.fixingCalendar(),
                index_.businessDayConvention(),
                index_.endOfMonth(),
                termStructure_.link.dayCounter(),
                termStructure_);

            List<double> nominals = new InitializedList<double>(1, 1.0);

            var floatSchedule = new Schedule(startDate, maturity,
                index_.tenor(), index_.fixingCalendar(),
                index_.businessDayConvention(),
                index_.businessDayConvention(),
                DateGeneration.Rule.Forward, false);
            List<CashFlow> floatingLeg = new IborLeg(floatSchedule, index_)
                .withFixingDays(0)
                .withNotionals(nominals)
                .withPaymentAdjustment(index_.businessDayConvention());

            var fixedSchedule = new Schedule(startDate, maturity, new Period(fixedLegFrequency_),
                index_.fixingCalendar(),
                BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Forward, false);
            List<CashFlow> fixedLeg = new FixedRateLeg(fixedSchedule)
                .withCouponRates(fixedRate, fixedLegDayCounter_)
                .withNotionals(nominals)
                .withPaymentAdjustment(index_.businessDayConvention());

            var swap = new Swap(floatingLeg, fixedLeg);
            swap.setPricingEngine(new DiscountingSwapEngine(termStructure_, false));
            var fairRate = fixedRate - (double)(swap.NPV() / (swap.legBPS(1) / 1.0e-4));
            cap_ = new Cap(floatingLeg, new InitializedList<double>(1, fairRate));

            base.performCalculations();
        }
    }
}
