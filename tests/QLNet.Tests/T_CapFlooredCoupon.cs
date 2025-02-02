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
using System;
using System.Collections.Generic;
using QLNet.Cashflows;
using Xunit;
using QLNet.Instruments;
using QLNet.Indexes;
using QLNet.Indexes.Ibor;
using QLNet.PricingEngines.CapFloor;
using QLNet.PricingEngines.Swap;
using QLNet.Time;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_CapFlooredCoupon
    {
        private class CommonVars
        {
            // global data
            public Date today, settlement, startDate;
            public Calendar calendar;
            public double nominal;
            public List<double> nominals;
            public BusinessDayConvention convention;
            public Frequency frequency;
            public IborIndex index;
            public int settlementDays, fixingDays;
            public RelinkableHandle<YieldTermStructure> termStructure;
            //public List<double> caps;
            //public List<double> floors;
            public int length;
            public double volatility;

            // cleanup
            SavedSettings backup;

            // setup
            public CommonVars()
            {
                termStructure = new RelinkableHandle<YieldTermStructure>();
                backup = new SavedSettings();
                length = 20;           //years
                volatility = 0.20;
                nominal = 100.0;
                nominals = new InitializedList<double>(length, nominal);
                frequency = Frequency.Annual;
                index = new Euribor1Y(termStructure);
                calendar = index.fixingCalendar();
                convention = BusinessDayConvention.ModifiedFollowing;
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                settlementDays = 2;
                fixingDays = 2;
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
                startDate = settlement;
                termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new ActualActual(ActualActual.Convention.ISDA)));
            }

            // utilities
            public List<CashFlow> makeFixedLeg(Date sDate, int len)
            {
                var endDate = calendar.advance(sDate, len, TimeUnit.Years, convention);
                var schedule = new Schedule(sDate, endDate, new Period(frequency), calendar,
                                                 convention, convention, DateGeneration.Rule.Forward, false);
                List<double> coupons = new InitializedList<double>(len, 0.0);
                return new FixedRateLeg(schedule)
                       .withCouponRates(coupons, new Thirty360())
                       .withNotionals(nominals);
            }

            public List<CashFlow> makeFloatingLeg(Date sDate, int len, double gearing = 1.0, double spread = 0.0)
            {
                var endDate = calendar.advance(sDate, len, TimeUnit.Years, convention);
                var schedule = new Schedule(sDate, endDate, new Period(frequency), calendar,
                                                 convention, convention, DateGeneration.Rule.Forward, false);
                List<double> gearingVector = new InitializedList<double>(len, gearing);
                List<double> spreadVector = new InitializedList<double>(len, spread);
                return new IborLeg(schedule, index)
                       .withPaymentDayCounter(index.dayCounter())
                       .withFixingDays(fixingDays)
                       .withGearings(gearingVector)
                       .withSpreads(spreadVector)
                       .withNotionals(nominals)
                       .withPaymentAdjustment(convention);
            }

            public List<CashFlow> makeCapFlooredLeg(Date sDate, int len, List<double?> caps, List<double?> floors,
                                                    double volatility, double gearing = 1.0, double spread = 0.0)
            {
                var endDate = calendar.advance(sDate, len, TimeUnit.Years, convention);
                var schedule = new Schedule(sDate, endDate, new Period(frequency), calendar,
                                                 convention, convention, DateGeneration.Rule.Forward, false);
                var vol = new Handle<OptionletVolatilityStructure>(new
                      ConstantOptionletVolatility(0, calendar, BusinessDayConvention.Following, volatility, new Actual365Fixed()));
                IborCouponPricer pricer = new BlackIborCouponPricer(vol);
                List<double> gearingVector = new InitializedList<double>(len, gearing);
                List<double> spreadVector = new InitializedList<double>(len, spread);

                List<CashFlow> iborLeg = new IborLeg(schedule, index)
                .withFloors(floors)
                .withPaymentDayCounter(index.dayCounter())
                .withFixingDays(fixingDays)
                .withGearings(gearingVector)
                .withSpreads(spreadVector)
                .withCaps(caps)
                .withNotionals(nominals)
                .withPaymentAdjustment(convention);
                Cashflows.Utils.setCouponPricer(iborLeg, pricer);
                return iborLeg;
            }

            public IPricingEngine makeEngine(double vols)
            {
                var vol = new Handle<Quote>(new SimpleQuote(vols));
                return new BlackCapFloorEngine(termStructure, vol);
            }

            public CapFloor makeCapFloor(CapFloorType type, List<CashFlow> leg, double capStrike,
                                         double floorStrike, double vol)
            {
                CapFloor result = null;
                switch (type)
                {
                    case CapFloorType.Cap:
                        result = new Cap(leg, new InitializedList<double>(1, capStrike));
                        break;
                    case CapFloorType.Floor:
                        result = new Floor(leg, new InitializedList<double>(1, floorStrike));
                        break;
                    case CapFloorType.Collar:
                        result = new Collar(leg, new InitializedList<double>(1, capStrike),
                                            new InitializedList<double>(1, floorStrike));
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown cap/floor ExerciseType");
                        break;
                }
                result.setPricingEngine(makeEngine(vol));
                return result;
            }

        }

        [Fact]
        public void testLargeRates()
        {
            // Testing degenerate collared coupon

            var vars = new CommonVars();

            /* A vanilla floating leg and a capped floating leg with strike
               equal to 100 and floor equal to 0 must have (about) the same NPV
               (depending on variance: option expiry and volatility)
            */

            List<double?> caps = new InitializedList<double?>(vars.length, 100.0);
            List<double?> floors = new InitializedList<double?>(vars.length, 0.0);
            var tolerance = 1e-10;

            // fixed leg with zero rate
            var fixedLeg = vars.makeFixedLeg(vars.startDate, vars.length);
            var floatLeg = vars.makeFloatingLeg(vars.startDate, vars.length);
            var collaredLeg = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors, vars.volatility);

            IPricingEngine engine = new DiscountingSwapEngine(vars.termStructure);
            var vanillaLeg = new Swap(fixedLeg, floatLeg);
            var collarLeg = new Swap(fixedLeg, collaredLeg);
            vanillaLeg.setPricingEngine(engine);
            collarLeg.setPricingEngine(engine);
            var npvVanilla = vanillaLeg.NPV();
            var npvCollar = collarLeg.NPV();
            if (System.Math.Abs(npvVanilla - npvCollar) > tolerance)
            {
                QAssert.Fail("Lenght: " + vars.length + " y" + "\n" +
                             "Volatility: " + vars.volatility * 100 + "%\n" +
                             "Notional: " + vars.nominal + "\n" +
                             "Vanilla floating leg NPV: " + vanillaLeg.NPV()
                             + "\n" +
                             "Collared floating leg NPV (strikes 0 and 100): "
                             + collarLeg.NPV()
                             + "\n" +
                             "Diff: " + System.Math.Abs(vanillaLeg.NPV() - collarLeg.NPV()));
            }
        }

        [Fact]
        public void testDecomposition()
        {
            // Testing collared coupon against its decomposition

            var vars = new CommonVars();

            var tolerance = 1e-12;
            double npvVanilla, npvCappedLeg, npvFlooredLeg, npvCollaredLeg, npvCap, npvFloor, npvCollar;
            double error;
            var floorstrike = 0.05;
            var capstrike = 0.10;
            List<double?> caps = new InitializedList<double?>(vars.length, capstrike);
            var caps0 = new List<double?>();
            List<double?> floors = new InitializedList<double?>(vars.length, floorstrike);
            var floors0 = new List<double?>();
            var gearing_p = 0.5;
            var spread_p = 0.002;
            var gearing_n = -1.5;
            var spread_n = 0.12;
            // fixed leg with zero rate
            var fixedLeg = vars.makeFixedLeg(vars.startDate, vars.length);
            // floating leg with gearing=1 and spread=0
            var floatLeg = vars.makeFloatingLeg(vars.startDate, vars.length);
            // floating leg with positive gearing (gearing_p) and spread<>0
            var floatLeg_p = vars.makeFloatingLeg(vars.startDate, vars.length, gearing_p, spread_p);
            // floating leg with negative gearing (gearing_n) and spread<>0
            var floatLeg_n = vars.makeFloatingLeg(vars.startDate, vars.length, gearing_n, spread_n);
            // Swap with null fixed leg and floating leg with gearing=1 and spread=0
            var vanillaLeg = new Swap(fixedLeg, floatLeg);
            // Swap with null fixed leg and floating leg with positive gearing and spread<>0
            var vanillaLeg_p = new Swap(fixedLeg, floatLeg_p);
            // Swap with null fixed leg and floating leg with negative gearing and spread<>0
            var vanillaLeg_n = new Swap(fixedLeg, floatLeg_n);

            IPricingEngine engine = new DiscountingSwapEngine(vars.termStructure);
            vanillaLeg.setPricingEngine(engine);
            vanillaLeg_p.setPricingEngine(engine);
            vanillaLeg_n.setPricingEngine(engine);

            /* CAPPED coupon - Decomposition of payoff
               Payoff = Nom * Min(rate,strike) * accrualperiod =
                     = Nom * [rate + Min(0,strike-rate)] * accrualperiod =
                     = Nom * rate * accrualperiod - Nom * Max(rate-strike,0) * accrualperiod =
                     = VanillaFloatingLeg - Call
            */

            // Case gearing = 1 and spread = 0
            var cappedLeg = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors0, vars.volatility);
            var capLeg = new Swap(fixedLeg, cappedLeg);
            capLeg.setPricingEngine(engine);
            var cap = new Cap(floatLeg, new InitializedList<double>(1, capstrike));
            cap.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg.NPV();
            npvCappedLeg = capLeg.NPV();
            npvCap = cap.NPV();
            error = System.Math.Abs(npvCappedLeg - (npvVanilla - npvCap));
            if (error > tolerance)
            {
                QAssert.Fail("\nCapped Leg: gearing=1, spread=0%, strike=" + capstrike * 100 +
                             "%\n" +
                             "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                             "  Floating Leg NPV - Cap NPV: " + (npvVanilla - npvCap) + "\n" +
                             "  Diff: " + error);
            }

            /* gearing = 1 and spread = 0
               FLOORED coupon - Decomposition of payoff
               Payoff = Nom * Max(rate,strike) * accrualperiod =
                     = Nom * [rate + Max(0,strike-rate)] * accrualperiod =
                     = Nom * rate * accrualperiod + Nom * Max(strike-rate,0) * accrualperiod =
                     = VanillaFloatingLeg + Put
            */

            var flooredLeg = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps0, floors, vars.volatility);
            var floorLeg = new Swap(fixedLeg, flooredLeg);
            floorLeg.setPricingEngine(engine);
            var floor = new Floor(floatLeg, new InitializedList<double>(1, floorstrike));
            floor.setPricingEngine(vars.makeEngine(vars.volatility));
            npvFlooredLeg = floorLeg.NPV();
            npvFloor = floor.NPV();
            error = System.Math.Abs(npvFlooredLeg - (npvVanilla + npvFloor));
            if (error > tolerance)
            {
                QAssert.Fail("Floored Leg: gearing=1, spread=0%, strike=" + floorstrike * 100 +
                             "%\n" +
                             "  Floored Floating Leg NPV: " + npvFlooredLeg + "\n" +
                             "  Floating Leg NPV + Floor NPV: " + (npvVanilla + npvFloor) + "\n" +
                             "  Diff: " + error);
            }

            /* gearing = 1 and spread = 0
               COLLARED coupon - Decomposition of payoff
               Payoff = Nom * Min(strikem,Max(rate,strikeM)) * accrualperiod =
                     = VanillaFloatingLeg - Collar
            */

            var collaredLeg = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors, vars.volatility);
            var collarLeg = new Swap(fixedLeg, collaredLeg);
            collarLeg.setPricingEngine(engine);
            var collar = new Collar(floatLeg, new InitializedList<double>(1, capstrike),
                                       new InitializedList<double>(1, floorstrike));
            collar.setPricingEngine(vars.makeEngine(vars.volatility));
            npvCollaredLeg = collarLeg.NPV();
            npvCollar = collar.NPV();
            error = System.Math.Abs(npvCollaredLeg - (npvVanilla - npvCollar));
            if (error > tolerance)
            {
                QAssert.Fail("\nCollared Leg: gearing=1, spread=0%, strike=" +
                             floorstrike * 100 + "% and " + capstrike * 100 + "%\n" +
                             "  Collared Floating Leg NPV: " + npvCollaredLeg + "\n" +
                             "  Floating Leg NPV - Collar NPV: " + (npvVanilla - npvCollar) + "\n" +
                             "  Diff: " + error);
            }

            /* gearing = a and spread = b
               CAPPED coupon - Decomposition of payoff
               Payoff
               = Nom * Min(a*rate+b,strike) * accrualperiod =
               = Nom * [a*rate+b + Min(0,strike-a*rate-b)] * accrualperiod =
               = Nom * a*rate+b * accrualperiod + Nom * Min(strike-b-a*rate,0) * accrualperiod
               --> If a>0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg - Call(a*rate+b,strike)
               --> If a<0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg + Nom * Min(strike-b+|a|*rate+,0) * accrualperiod =
                        = VanillaFloatingLeg + Put(|a|*rate+b,strike)
            */

            // Positive gearing
            var cappedLeg_p = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors0,
                                                                vars.volatility, gearing_p, spread_p);
            var capLeg_p = new Swap(fixedLeg, cappedLeg_p);
            capLeg_p.setPricingEngine(engine);
            var cap_p = new Cap(floatLeg_p, new InitializedList<double>(1, capstrike));
            cap_p.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_p.NPV();
            npvCappedLeg = capLeg_p.NPV();
            npvCap = cap_p.NPV();
            error = System.Math.Abs(npvCappedLeg - (npvVanilla - npvCap));
            if (error > tolerance)
            {
                QAssert.Fail("\nCapped Leg: gearing=" + gearing_p + ", " +
                             "spread= " + spread_p * 100 +
                             "%, strike=" + capstrike * 100 + "%, " +
                             "effective strike= " + (capstrike - spread_p) / gearing_p * 100 +
                             "%\n" +
                             "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                             "  Vanilla Leg NPV: " + npvVanilla + "\n" +
                             "  Cap NPV: " + npvCap + "\n" +
                             "  Floating Leg NPV - Cap NPV: " + (npvVanilla - npvCap) + "\n" +
                             "  Diff: " + error);
            }

            // Negative gearing
            var cappedLeg_n = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors0,
                                                                vars.volatility, gearing_n, spread_n);
            var capLeg_n = new Swap(fixedLeg, cappedLeg_n);
            capLeg_n.setPricingEngine(engine);
            var floor_n = new Floor(floatLeg, new InitializedList<double>(1, (capstrike - spread_n) / gearing_n));
            floor_n.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_n.NPV();
            npvCappedLeg = capLeg_n.NPV();
            npvFloor = floor_n.NPV();
            error = System.Math.Abs(npvCappedLeg - (npvVanilla + gearing_n * npvFloor));
            if (error > tolerance)
            {
                QAssert.Fail("\nCapped Leg: gearing=" + gearing_n + ", " +
                             "spread= " + spread_n * 100 +
                             "%, strike=" + capstrike * 100 + "%, " +
                             "effective strike= " + (capstrike - spread_n) / gearing_n * 100 +
                             "%\n" +
                             "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                             "  npv Vanilla: " + npvVanilla + "\n" +
                             "  npvFloor: " + npvFloor + "\n" +
                             "  Floating Leg NPV - Cap NPV: " + (npvVanilla + gearing_n * npvFloor) + "\n" +
                             "  Diff: " + error);
            }

            /* gearing = a and spread = b
               FLOORED coupon - Decomposition of payoff
               Payoff
               = Nom * Max(a*rate+b,strike) * accrualperiod =
               = Nom * [a*rate+b + Max(0,strike-a*rate-b)] * accrualperiod =
               = Nom * a*rate+b * accrualperiod + Nom * Max(strike-b-a*rate,0) * accrualperiod
               --> If a>0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg + Put(a*rate+b,strike)
               --> If a<0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg + Nom * Max(strike-b+|a|*rate+,0) * accrualperiod =
                        = VanillaFloatingLeg - Call(|a|*rate+b,strike)
            */

            // Positive gearing
            var flooredLeg_p1 = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps0, floors,
                                                                  vars.volatility, gearing_p, spread_p);
            var floorLeg_p1 = new Swap(fixedLeg, flooredLeg_p1);
            floorLeg_p1.setPricingEngine(engine);
            var floor_p1 = new Floor(floatLeg_p, new InitializedList<double>(1, floorstrike));
            floor_p1.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_p.NPV();
            npvFlooredLeg = floorLeg_p1.NPV();
            npvFloor = floor_p1.NPV();
            error = System.Math.Abs(npvFlooredLeg - (npvVanilla + npvFloor));
            if (error > tolerance)
            {
                QAssert.Fail("\nFloored Leg: gearing=" + gearing_p + ", "
                             + "spread= " + spread_p * 100 + "%, strike=" + floorstrike * 100 + "%, "
                             + "effective strike= " + (floorstrike - spread_p) / gearing_p * 100
                             + "%\n" +
                             "  Floored Floating Leg NPV: " + npvFlooredLeg
                             + "\n" +
                             "  Floating Leg NPV + Floor NPV: " + (npvVanilla + npvFloor)
                             + "\n" +
                             "  Diff: " + error);
            }
            // Negative gearing
            var flooredLeg_n = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps0, floors,
                                                                 vars.volatility, gearing_n, spread_n);
            var floorLeg_n = new Swap(fixedLeg, flooredLeg_n);
            floorLeg_n.setPricingEngine(engine);
            var cap_n = new Cap(floatLeg, new InitializedList<double>(1, (floorstrike - spread_n) / gearing_n));
            cap_n.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_n.NPV();
            npvFlooredLeg = floorLeg_n.NPV();
            npvCap = cap_n.NPV();
            error = System.Math.Abs(npvFlooredLeg - (npvVanilla - gearing_n * npvCap));
            if (error > tolerance)
            {
                QAssert.Fail("\nCapped Leg: gearing=" + gearing_n + ", " +
                             "spread= " + spread_n * 100 +
                             "%, strike=" + floorstrike * 100 + "%, " +
                             "effective strike= " + (floorstrike - spread_n) / gearing_n * 100 +
                             "%\n" +
                             "  Capped Floating Leg NPV: " + npvFlooredLeg + "\n" +
                             "  Floating Leg NPV - Cap NPV: " + (npvVanilla - gearing_n * npvCap) + "\n" +
                             "  Diff: " + error);
            }
            /* gearing = a and spread = b
               COLLARED coupon - Decomposition of payoff
               Payoff = Nom * Min(caprate,Max(a*rate+b,floorrate)) * accrualperiod
               --> If a>0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg - Collar(a*rate+b, floorrate, caprate)
               --> If a<0 (assuming positive effective strike):
                  Payoff = VanillaFloatingLeg + Collar(|a|*rate+b, caprate, floorrate)
            */
            // Positive gearing
            var collaredLeg_p = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors,
                                                                  vars.volatility, gearing_p, spread_p);
            var collarLeg_p1 = new Swap(fixedLeg, collaredLeg_p);
            collarLeg_p1.setPricingEngine(engine);
            var collar_p = new Collar(floatLeg_p, new InitializedList<double>(1, capstrike),
                                         new InitializedList<double>(1, floorstrike));
            collar_p.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_p.NPV();
            npvCollaredLeg = collarLeg_p1.NPV();
            npvCollar = collar_p.NPV();
            error = System.Math.Abs(npvCollaredLeg - (npvVanilla - npvCollar));
            if (error > tolerance)
            {
                QAssert.Fail("\nCollared Leg: gearing=" + gearing_p + ", "
                             + "spread= " + spread_p * 100 + "%, strike="
                             + floorstrike * 100 + "% and " + capstrike * 100
                             + "%, "
                             + "effective strike=" + (floorstrike - spread_p) / gearing_p * 100
                             + "% and " + (capstrike - spread_p) / gearing_p * 100
                             + "%\n" +
                             "  Collared Floating Leg NPV: " + npvCollaredLeg
                             + "\n" +
                             "  Floating Leg NPV - Collar NPV: " + (npvVanilla - npvCollar)
                             + "\n" +
                             "  Diff: " + error);
            }
            // Negative gearing
            var collaredLeg_n = vars.makeCapFlooredLeg(vars.startDate, vars.length, caps, floors,
                                                                  vars.volatility, gearing_n, spread_n);
            var collarLeg_n1 = new Swap(fixedLeg, collaredLeg_n);
            collarLeg_n1.setPricingEngine(engine);
            var collar_n = new Collar(floatLeg, new InitializedList<double>(1, (floorstrike - spread_n) / gearing_n),
                                         new InitializedList<double>(1, (capstrike - spread_n) / gearing_n));
            collar_n.setPricingEngine(vars.makeEngine(vars.volatility));
            npvVanilla = vanillaLeg_n.NPV();
            npvCollaredLeg = collarLeg_n1.NPV();
            npvCollar = collar_n.NPV();
            error = System.Math.Abs(npvCollaredLeg - (npvVanilla - gearing_n * npvCollar));
            if (error > tolerance)
            {
                QAssert.Fail("\nCollared Leg: gearing=" + gearing_n + ", "
                             + "spread= " + spread_n * 100 + "%, strike="
                             + floorstrike * 100 + "% and " + capstrike * 100
                             + "%, "
                             + "effective strike=" + (floorstrike - spread_n) / gearing_n * 100
                             + "% and " + (capstrike - spread_n) / gearing_n * 100
                             + "%\n" +
                             "  Collared Floating Leg NPV: " + npvCollaredLeg
                             + "\n" +
                             "  Floating Leg NPV - Collar NPV: " + (npvVanilla - gearing_n * npvCollar)
                             + "\n" +
                             "  Diff: " + error);
            }
        }
    }
}
