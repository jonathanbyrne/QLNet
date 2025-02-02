﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.PricingEngines.CapFloor;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! helper class
    /*! This class provides a more comfortable way
        to instantiate standard market cap and floor.
    */
    [PublicAPI]
    public class MakeCapFloor
    {
        private CapFloorType capFloorType_;
        private IPricingEngine engine_;
        private bool firstCapletExcluded_, asOptionlet_;
        private MakeVanillaSwap makeVanillaSwap_;
        private double? strike_;

        public MakeCapFloor(CapFloorType capFloorType, Period tenor, IborIndex iborIndex, double? strike = null,
            Period forwardStart = null)
        {
            capFloorType_ = capFloorType;
            strike_ = strike;
            firstCapletExcluded_ = forwardStart == new Period(0, TimeUnit.Days);
            asOptionlet_ = false;
            makeVanillaSwap_ = new MakeVanillaSwap(tenor, iborIndex, 0.0, forwardStart);
        }

        public static implicit operator CapFloor(MakeCapFloor o) => o.value();

        //! only get last coupon
        public MakeCapFloor asOptionlet(bool b = true)
        {
            asOptionlet_ = b;
            return this;
        }

        public CapFloor value()
        {
            VanillaSwap swap = makeVanillaSwap_;

            var leg = swap.floatingLeg();
            if (firstCapletExcluded_)
            {
                leg.RemoveAt(0);
            }

            // only leaves the last coupon
            if (asOptionlet_ && leg.Count > 1)
            {
                leg.RemoveRange(0, leg.Count - 2); // Sun Studio needs an lvalue
            }

            List<double> strikeVector;
            if (strike_ == null)
            {
                // temporary patch...
                // should be fixed for every CapFloor::Engine
                var temp = engine_ as BlackCapFloorEngine;
                QLNet.Utils.QL_REQUIRE(temp != null, () => "cannot calculate ATM without a BlackCapFloorEngine");
                var discountCurve = temp.termStructure();
                strikeVector = new InitializedList<double>(1, CashFlows.atmRate(leg, discountCurve, false, discountCurve.link.referenceDate()));
            }
            else
            {
                strikeVector = new InitializedList<double>(1, strike_.Value);
            }

            var capFloor = new CapFloor(capFloorType_, leg, strikeVector);
            capFloor.setPricingEngine(engine_);
            return capFloor;
        }

        public MakeCapFloor withCalendar(Calendar cal)
        {
            makeVanillaSwap_.withFixedLegCalendar(cal);
            makeVanillaSwap_.withFloatingLegCalendar(cal);
            return this;
        }

        public MakeCapFloor withConvention(BusinessDayConvention bdc)
        {
            makeVanillaSwap_.withFixedLegConvention(bdc);
            makeVanillaSwap_.withFloatingLegConvention(bdc);
            return this;
        }

        public MakeCapFloor withDayCount(DayCounter dc)
        {
            makeVanillaSwap_.withFixedLegDayCount(dc);
            makeVanillaSwap_.withFloatingLegDayCount(dc);
            return this;
        }

        public MakeCapFloor withEffectiveDate(Date effectiveDate, bool firstCapletExcluded)
        {
            makeVanillaSwap_.withEffectiveDate(effectiveDate);
            firstCapletExcluded_ = firstCapletExcluded;
            return this;
        }

        public MakeCapFloor withEndOfMonth(bool flag = true)
        {
            makeVanillaSwap_.withFixedLegEndOfMonth(flag);
            makeVanillaSwap_.withFloatingLegEndOfMonth(flag);
            return this;
        }

        public MakeCapFloor withFirstDate(Date d)
        {
            makeVanillaSwap_.withFixedLegFirstDate(d);
            makeVanillaSwap_.withFloatingLegFirstDate(d);
            return this;
        }

        public MakeCapFloor withNextToLastDate(Date d)
        {
            makeVanillaSwap_.withFixedLegNextToLastDate(d);
            makeVanillaSwap_.withFloatingLegNextToLastDate(d);
            return this;
        }

        public MakeCapFloor withNominal(double n)
        {
            makeVanillaSwap_.withNominal(n);
            return this;
        }

        public MakeCapFloor withPricingEngine(IPricingEngine engine)
        {
            engine_ = engine;
            return this;
        }

        public MakeCapFloor withRule(DateGeneration.Rule r)
        {
            makeVanillaSwap_.withFixedLegRule(r);
            makeVanillaSwap_.withFloatingLegRule(r);
            return this;
        }

        public MakeCapFloor withTenor(Period t)
        {
            makeVanillaSwap_.withFixedLegTenor(t);
            makeVanillaSwap_.withFloatingLegTenor(t);
            return this;
        }

        public MakeCapFloor withTerminationDateConvention(BusinessDayConvention bdc)
        {
            makeVanillaSwap_.withFixedLegTerminationDateConvention(bdc);
            makeVanillaSwap_.withFloatingLegTerminationDateConvention(bdc);
            return this;
        }
    }
}
