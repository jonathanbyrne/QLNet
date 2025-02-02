﻿/*
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Math.Solvers1d;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Instruments
{
    /// <summary>
    ///     Base class for cap-like instruments
    ///     \ingroup instruments
    ///     \test
    ///     - the correctness of the returned value is tested by checking
    ///     that the price of a cap (resp. floor) decreases
    ///     (resp. increases) with the strike rate.
    ///     - the relationship between the values of caps, floors and the
    ///     resulting collars is checked.
    ///     - the put-call parity between the values of caps, floors and
    ///     swaps is checked.
    ///     - the correctness of the returned implied volatility is tested
    ///     by using it for reproducing the target value.
    ///     - the correctness of the returned value is tested by checking
    ///     it against a known good value.
    /// </summary>
    [PublicAPI]
    public class CapFloor : Instrument
    {
        #region Pricing

        [PublicAPI]
        public class Arguments : IPricingEngineArguments
        {
            public List<double> accrualTimes { get; set; }

            public List<double?> capRates { get; set; }

            public List<Date> endDates { get; set; }

            public List<Date> fixingDates { get; set; }

            public List<double?> floorRates { get; set; }

            public List<double?> forwards { get; set; }

            public List<double> gearings { get; set; }

            public List<double> nominals { get; set; }

            public List<double> spreads { get; set; }

            public List<Date> startDates { get; set; }

            public CapFloorType type { get; set; }

            public void validate()
            {
                if (endDates.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of end dates ("
                                                                          + endDates.Count + ")");
                }

                if (accrualTimes.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of  accrual times  ("
                                                                          + accrualTimes.Count + ")");
                }

                if (capRates.Count != startDates.Count && type != CapFloorType.Floor)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of  of cap rates  ("
                                                                          + capRates.Count + ")");
                }

                if (floorRates.Count != startDates.Count && type != CapFloorType.Cap)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of  of floor rates  ("
                                                                          + floorRates.Count + ")");
                }

                if (gearings.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of  gearings ("
                                                                          + gearings.Count + ")");
                }

                if (spreads.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of spreads ("
                                                                          + spreads.Count + ")");
                }

                if (nominals.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of nominals ("
                                                                          + nominals.Count + ")");
                }

                if (forwards.Count != startDates.Count)
                {
                    throw new ArgumentException("number of start dates (" + startDates.Count
                                                                          + ") different from that of forwards ("
                                                                          + forwards.Count + ")");
                }
            }
        }

        #endregion

        #region Private Attributes

        private CapFloorType type_;
        private List<CashFlow> floatingLeg_;
        private List<double> capRates_;
        private List<double> floorRates_;

        #endregion

        #region Constructors

        public CapFloor(CapFloorType type, List<CashFlow> floatingLeg, List<double> capRates, List<double> floorRates)
        {
            type_ = type;
            floatingLeg_ = new List<CashFlow>(floatingLeg);
            capRates_ = new List<double>(capRates);
            floorRates_ = new List<double>(floorRates);

            if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar)
            {
                if (capRates_.Count == 0)
                {
                    throw new ArgumentException("no cap rates given");
                }

                while (capRates_.Count < floatingLeg_.Count)
                {
                    capRates_.Add(capRates_.Last());
                }
            }

            if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar)
            {
                if (floorRates_.Count == 0)
                {
                    throw new ArgumentException("no floor rates given");
                }

                while (floorRates_.Count < floatingLeg_.Count)
                {
                    floorRates_.Add(floorRates_.Last());
                }
            }

            for (var i = 0; i < floatingLeg_.Count; i++)
            {
                floatingLeg_[i].registerWith(update);
            }

            Settings.registerWith(update);
        }

        public CapFloor(CapFloorType type, List<CashFlow> floatingLeg, List<double> strikes)
        {
            type_ = type;
            floatingLeg_ = new List<CashFlow>(floatingLeg);

            if (strikes.Count == 0)
            {
                throw new ArgumentException("no strikes given");
            }

            if (type_ == CapFloorType.Cap)
            {
                capRates_ = new List<double>(strikes);

                while (capRates_.Count < floatingLeg_.Count)
                {
                    capRates_.Add(capRates_.Last());
                }
            }
            else if (type_ == CapFloorType.Floor)
            {
                floorRates_ = new List<double>(strikes);

                while (floorRates_.Count < floatingLeg_.Count)
                {
                    floorRates_.Add(floorRates_.Last());
                }
            }
            else
            {
                throw new ArgumentException("only Cap/Floor types allowed in this constructor");
            }

            for (var i = 0; i < floatingLeg_.Count; i++)
            {
                floatingLeg_[i].registerWith(update);
            }

            Settings.registerWith(update);
        }

        #endregion

        #region Instrument interface

        public override bool isExpired()
        {
            var today = Settings.evaluationDate();
            foreach (var cf in floatingLeg_)
            {
                if (!cf.hasOccurred(today))
                {
                    return false;
                }
            }

            return true;
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            if (!(args is Arguments arguments))
            {
                throw new ArgumentException("wrong argument ExerciseType");
            }

            var n = floatingLeg_.Count;

            arguments.startDates = new InitializedList<Date>(n);
            arguments.fixingDates = new InitializedList<Date>(n);
            arguments.endDates = new InitializedList<Date>(n);
            arguments.accrualTimes = new InitializedList<double>(n);
            arguments.forwards = new InitializedList<double?>(n);
            arguments.nominals = new InitializedList<double>(n);
            arguments.gearings = new InitializedList<double>(n);
            arguments.capRates = new InitializedList<double?>(n);
            arguments.floorRates = new InitializedList<double?>(n);
            arguments.spreads = new InitializedList<double>(n);

            arguments.type = type_;

            var today = Settings.evaluationDate();

            for (var i = 0; i < n; ++i)
            {
                var coupon = floatingLeg_[i] as FloatingRateCoupon;

                if (coupon == null)
                {
                    throw new ArgumentException("non-FloatingRateCoupon given");
                }

                arguments.startDates[i] = coupon.accrualStartDate();
                arguments.fixingDates[i] = coupon.fixingDate();
                arguments.endDates[i] = coupon.date();

                // this is passed explicitly for precision
                arguments.accrualTimes[i] = coupon.accrualPeriod();

                // this is passed explicitly for precision...
                if (arguments.endDates[i] >= today)
                {
                    // ...but only if needed
                    arguments.forwards[i] = coupon.adjustedFixing;
                }
                else
                {
                    arguments.forwards[i] = null;
                }

                arguments.nominals[i] = coupon.nominal();
                var spread = coupon.spread();
                var gearing = coupon.gearing();
                arguments.gearings[i] = gearing;
                arguments.spreads[i] = spread;

                if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar)
                {
                    arguments.capRates[i] = (capRates_[i] - spread) / gearing;
                }
                else
                {
                    arguments.capRates[i] = null;
                }

                if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar)
                {
                    arguments.floorRates[i] = (floorRates_[i] - spread) / gearing;
                }
                else
                {
                    arguments.floorRates[i] = null;
                }
            }
        }

        #endregion

        #region Inspectors

        public CapFloorType getCapFloorType() => type_;

        public List<double> capRates() => capRates_;

        public List<double> floorRates() => floorRates_;

        public List<CashFlow> floatingLeg() => floatingLeg_;

        public Date startDate() => CashFlows.startDate(floatingLeg_);

        public Date maturityDate() => CashFlows.maturityDate(floatingLeg_);

        public FloatingRateCoupon lastFloatingRateCoupon()
        {
            var lastCF = floatingLeg_.Last();
            var lastFloatingCoupon = lastCF as FloatingRateCoupon;
            return lastFloatingCoupon;
        }

        public CapFloor optionlet(int i)
        {
            if (i >= floatingLeg().Count)
            {
                throw new ArgumentException(i + " optionlet does not exist, only " +
                                            floatingLeg().Count);
            }

            var cf = new List<CashFlow>();
            cf.Add(floatingLeg()[i]);

            var cap = new List<double>();
            var floor = new List<double>();

            if (getCapFloorType() == CapFloorType.Cap || getCapFloorType() == CapFloorType.Collar)
            {
                cap.Add(capRates()[i]);
            }

            if (getCapFloorType() == CapFloorType.Floor || getCapFloorType() == CapFloorType.Collar)
            {
                floor.Add(floorRates()[i]);
            }

            return new CapFloor(getCapFloorType(), cf, cap, floor);
        }

        public double atmRate(YieldTermStructure discountCurve)
        {
            var includeSettlementDateFlows = false;
            var settlementDate = discountCurve.referenceDate();
            return CashFlows.atmRate(floatingLeg_, discountCurve, includeSettlementDateFlows, settlementDate);
        }

        public double impliedVolatility(
            double targetValue,
            Handle<YieldTermStructure> discountCurve,
            double guess,
            double accuracy,
            int maxEvaluations) =>
            impliedVolatility(targetValue, discountCurve, guess, accuracy, maxEvaluations,
                1.0e-7, 4.0, VolatilityType.ShiftedLognormal, 0.0);

        public double impliedVolatility(
            double targetValue,
            Handle<YieldTermStructure> discountCurve,
            double guess,
            double accuracy,
            int maxEvaluations,
            double minVol,
            double maxVol,
            VolatilityType type,
            double displacement)
        {
            calculate();
            if (isExpired())
            {
                throw new ArgumentException("instrument expired");
            }

            var f = new ImpliedVolHelper(this, discountCurve, targetValue, displacement, type);
            var solver = new NewtonSafe();
            solver.setMaxEvaluations(maxEvaluations);
            return solver.solve(f, accuracy, guess, minVol, maxVol);
        }

        #endregion
    }

    //! base class for cap/floor engines
}
