﻿/*
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
using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! Base class for yoy inflation cap-like instruments
    /*! \ingroup instruments

        Note that the standard YoY inflation cap/floor defined here is
        different from nominal, because in nominal world standard
        cap/floors do not have the first optionlet.  This is because
        they set in advance so there is no point.  However, yoy
        inflation generally sets (effectively) in arrears, (actually
        in arrears vs lag of a few months) thus the first optionlet is
        relevant.  Hence we can do a parity test without a special
        definition of the YoY cap/floor instrument.

        \test
        - the relationship between the values of caps, floors and the
          resulting collars is checked.
        - the put-call parity between the values of caps, floors and
          swaps is checked.
        - the correctness of the returned value is tested by checking
          it against a known good value.
    */

    [PublicAPI]
    public class YoYInflationCapFloor : Instrument
    {
        //! %Arguments for YoY Inflation cap/floor calculation
        [PublicAPI]
        public class Arguments : IPricingEngineArguments
        {
            public List<double> accrualTimes { get; set; }

            public List<double?> capRates { get; set; }

            public List<Date> fixingDates { get; set; }

            public List<double?> floorRates { get; set; }

            public List<double> gearings { get; set; }

            public YoYInflationIndex index { get; set; }

            public List<double> nominals { get; set; }

            public Period observationLag { get; set; }

            public List<Date> payDates { get; set; }

            public List<double> spreads { get; set; }

            public List<Date> startDates { get; set; }

            public CapFloorType type { get; set; }

            public void validate()
            {
                QLNet.Utils.QL_REQUIRE(payDates.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of pay dates ("
                                              + payDates.Count + ")");
                QLNet.Utils.QL_REQUIRE(accrualTimes.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of accrual times ("
                                              + accrualTimes.Count + ")");
                QLNet.Utils.QL_REQUIRE(type == CapFloorType.Floor ||
                                                capRates.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of cap rates ("
                                              + capRates.Count + ")");
                QLNet.Utils.QL_REQUIRE(type == CapFloorType.Cap ||
                                                floorRates.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of floor rates ("
                                              + floorRates.Count + ")");
                QLNet.Utils.QL_REQUIRE(gearings.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of gearings ("
                                              + gearings.Count + ")");
                QLNet.Utils.QL_REQUIRE(spreads.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of spreads ("
                                              + spreads.Count + ")");
                QLNet.Utils.QL_REQUIRE(nominals.Count == startDates.Count, () =>
                    "number of start dates (" + startDates.Count
                                              + ") different from that of nominals ("
                                              + nominals.Count + ")");
            }
        }

        //! base class for cap/floor engines
        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        private List<double> capRates_;
        private List<double> floorRates_;
        private CapFloorType type_;
        private List<CashFlow> yoyLeg_;

        public YoYInflationCapFloor(CapFloorType type, List<CashFlow> yoyLeg, List<double> capRates, List<double> floorRates)
        {
            type_ = type;
            yoyLeg_ = yoyLeg;
            capRates_ = capRates;
            floorRates_ = floorRates;

            if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar)
            {
                QLNet.Utils.QL_REQUIRE(!capRates_.empty(), () => "no cap rates given");
                while (capRates_.Count < yoyLeg_.Count)
                {
                    capRates_.Add(capRates_.Last());
                }
            }

            if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar)
            {
                QLNet.Utils.QL_REQUIRE(!floorRates_.empty(), () => "no floor rates given");
                while (floorRates_.Count < yoyLeg_.Count)
                {
                    floorRates_.Add(floorRates_.Last());
                }
            }

            foreach (var cf in yoyLeg_)
            {
                cf.registerWith(update);
            }

            Settings.registerWith(update);
        }

        public YoYInflationCapFloor(CapFloorType type, List<CashFlow> yoyLeg, List<double> strikes)
        {
            type_ = type;
            yoyLeg_ = yoyLeg;

            QLNet.Utils.QL_REQUIRE(!strikes.empty(), () => "no strikes given");
            if (type_ == CapFloorType.Cap)
            {
                capRates_ = strikes;
                while (capRates_.Count < yoyLeg_.Count)
                {
                    capRates_.Add(capRates_.Last());
                }
            }
            else if (type_ == CapFloorType.Floor)
            {
                floorRates_ = strikes;
                while (floorRates_.Count < yoyLeg_.Count)
                {
                    floorRates_.Add(floorRates_.Last());
                }
            }
            else
            {
                QLNet.Utils.QL_FAIL("only Cap/Floor types allowed in this constructor");
            }

            foreach (var cf in yoyLeg_)
            {
                cf.registerWith(update);
            }

            Settings.registerWith(update);
        }

        public virtual double atmRate(YieldTermStructure discountCurve) =>
            CashFlows.atmRate(yoyLeg_, discountCurve,
                false, discountCurve.referenceDate());

        public List<double> capRates() => capRates_;

        public List<double> floorRates() => floorRates_;

        //! implied term volatility
        public virtual double impliedVolatility(
            double price,
            Handle<YoYInflationTermStructure> yoyCurve,
            double guess,
            double accuracy = 1.0e-4,
            int maxEvaluations = 100,
            double minVol = 1.0e-7,
            double maxVol = 4.0)
        {
            QLNet.Utils.QL_FAIL("not implemented yet");
            return 0;
        }

        // Instrument interface
        public override bool isExpired()
        {
            for (var i = yoyLeg_.Count; i > 0; --i)
            {
                if (!yoyLeg_[i - 1].hasOccurred())
                {
                    return false;
                }
            }

            return true;
        }

        public YoYInflationCoupon lastYoYInflationCoupon()
        {
            var lastYoYInflationCoupon = yoyLeg_.Last() as YoYInflationCoupon;
            return lastYoYInflationCoupon;
        }

        public Date maturityDate() => CashFlows.maturityDate(yoyLeg_);

        //! Returns the n-th optionlet as a cap/floor with only one cash flow.
        public YoYInflationCapFloor optionlet(int i)
        {
            QLNet.Utils.QL_REQUIRE(i < yoyLeg().Count, () => " optionlet does not exist, only " + yoyLeg().Count);
            var cf = new List<CashFlow>();
            cf.Add(yoyLeg()[i]);

            List<double> cap = new List<double>(), floor = new List<double>();
            if (type() == CapFloorType.Cap || type() == CapFloorType.Collar)
            {
                cap.Add(capRates()[i]);
            }

            if (type() == CapFloorType.Floor || type() == CapFloorType.Collar)
            {
                floor.Add(floorRates()[i]);
            }

            return new YoYInflationCapFloor(type(), cf, cap, floor);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            var arguments = args as Arguments;
            QLNet.Utils.QL_REQUIRE(arguments != null, () => "wrong argument ExerciseType");

            var n = yoyLeg_.Count;

            arguments.startDates = new List<Date>(n);
            arguments.fixingDates = new List<Date>(n);
            arguments.payDates = new List<Date>(n);
            arguments.accrualTimes = new List<double>(n);
            arguments.nominals = new List<double>(n);
            arguments.gearings = new List<double>(n);
            arguments.capRates = new List<double?>(n);
            arguments.floorRates = new List<double?>(n);
            arguments.spreads = new List<double>(n);

            arguments.type = type_;

            for (var i = 0; i < n; ++i)
            {
                var coupon = yoyLeg_[i] as YoYInflationCoupon;
                QLNet.Utils.QL_REQUIRE(coupon != null, () => "non-YoYInflationCoupon given");
                arguments.startDates.Add(coupon.accrualStartDate());
                arguments.fixingDates.Add(coupon.fixingDate());
                arguments.payDates.Add(coupon.date());

                // this is passed explicitly for precision
                arguments.accrualTimes.Add(coupon.accrualPeriod());

                arguments.nominals.Add(coupon.nominal());
                var spread = coupon.spread();
                var gearing = coupon.gearing();
                arguments.gearings.Add(gearing);
                arguments.spreads.Add(spread);

                if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar)
                {
                    arguments.capRates.Add((capRates_[i] - spread) / gearing);
                }
                else
                {
                    arguments.capRates.Add(null);
                }

                if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar)
                {
                    arguments.floorRates.Add((floorRates_[i] - spread) / gearing);
                }
                else
                {
                    arguments.floorRates.Add(null);
                }
            }
        }

        public Date startDate() => CashFlows.startDate(yoyLeg_);

        // Inspectors
        public CapFloorType type() => type_;

        public List<CashFlow> yoyLeg() => yoyLeg_;
    }

    //! Concrete YoY Inflation cap class
    /*! \ingroup instruments */

    //! Concrete YoY Inflation floor class
    /*! \ingroup instruments */

    //! Concrete YoY Inflation collar class
    /*! \ingroup instruments */
}
