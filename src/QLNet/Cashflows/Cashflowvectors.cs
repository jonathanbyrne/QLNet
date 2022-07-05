/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet.Cashflows
{
    public static class CashFlowVectors
    {
        public static List<CashFlow> FloatingDigitalLeg<InterestRateIndexType, FloatingCouponType, DigitalCouponType>(
            List<double> nominals,
            Schedule schedule,
            InterestRateIndexType index,
            DayCounter paymentDayCounter,
            BusinessDayConvention paymentAdj,
            List<int> fixingDays,
            List<double> gearings,
            List<double> spreads,
            bool isInArrears,
            List<double> callStrikes,
            Position.Type callPosition,
            bool isCallATMIncluded,
            List<double> callDigitalPayoffs,
            List<double> putStrikes,
            Position.Type putPosition,
            bool isPutATMIncluded,
            List<double> putDigitalPayoffs,
            DigitalReplication replication)
            where InterestRateIndexType : InterestRateIndex, new()
            where FloatingCouponType : FloatingRateCoupon, new()
            where DigitalCouponType : DigitalCoupon, new()
        {
            var n = schedule.Count;
            QLNet.Utils.QL_REQUIRE(!nominals.empty(), () => "no notional given");
            QLNet.Utils.QL_REQUIRE(nominals.Count <= n, () => "too many nominals (" + nominals.Count + "), only " + n + " required");
            if (gearings != null)
            {
                QLNet.Utils.QL_REQUIRE(gearings.Count <= n, () => "too many gearings (" + gearings.Count + "), only " + n + " required");
            }

            if (spreads != null)
            {
                QLNet.Utils.QL_REQUIRE(spreads.Count <= n, () => "too many spreads (" + spreads.Count + "), only " + n + " required");
            }

            if (callStrikes != null)
            {
                QLNet.Utils.QL_REQUIRE(callStrikes.Count <= n, () => "too many nominals (" + callStrikes.Count + "), only " + n + " required");
            }

            if (putStrikes != null)
            {
                QLNet.Utils.QL_REQUIRE(putStrikes.Count <= n, () => "too many nominals (" + putStrikes.Count + "), only " + n + " required");
            }

            var leg = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule.date(i);
                refEnd = end = schedule.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdj);
                if (i == 0 && !schedule.isRegular(i + 1))
                {
                    var bdc = schedule.businessDayConvention();
                    refStart = calendar.adjust(end - schedule.tenor(), bdc);
                }

                if (i == n - 1 && !schedule.isRegular(i + 1))
                {
                    var bdc = schedule.businessDayConvention();
                    refEnd = calendar.adjust(start + schedule.tenor(), bdc);
                }

                if (gearings.Get(i, 1.0).IsEqual(0.0))
                {
                    // fixed coupon
                    leg.Add(new FixedRateCoupon(paymentDate, nominals.Get(i, 1.0),
                        spreads.Get(i, 1.0),
                        paymentDayCounter,
                        start, end, refStart, refEnd));
                }
                else
                {
                    // floating digital coupon
                    var underlying = FastActivator<FloatingCouponType>.Create().factory(
                        nominals.Get(i, 1.0),
                        paymentDate, start, end,
                        fixingDays.Get(i, index.fixingDays()),
                        index,
                        gearings.Get(i, 1.0),
                        spreads.Get(i, 0.0),
                        refStart, refEnd,
                        paymentDayCounter, isInArrears) as FloatingCouponType;

                    var digitalCoupon = FastActivator<DigitalCouponType>.Create().factory(
                        underlying,
                        Utils.toNullable(callStrikes.Get(i, double.MinValue)),
                        callPosition,
                        isCallATMIncluded,
                        Utils.toNullable(callDigitalPayoffs.Get(i, double.MinValue)),
                        Utils.toNullable(putStrikes.Get(i, double.MinValue)),
                        putPosition,
                        isPutATMIncluded,
                        Utils.toNullable(putDigitalPayoffs.Get(i, double.MinValue)),
                        replication) as DigitalCouponType;

                    leg.Add(digitalCoupon);
                }
            }

            return leg;
        }

        public static List<CashFlow> FloatingLeg<InterestRateIndexType, FloatingCouponType, CappedFlooredCouponType>(
            List<double> nominals,
            Schedule schedule,
            InterestRateIndexType index,
            DayCounter paymentDayCounter,
            BusinessDayConvention paymentAdj,
            List<int> fixingDays,
            List<double> gearings,
            List<double> spreads,
            List<double?> caps,
            List<double?> floors,
            bool isInArrears,
            bool isZero)
            where InterestRateIndexType : InterestRateIndex, new()
            where FloatingCouponType : FloatingRateCoupon, new()
            where CappedFlooredCouponType : CappedFlooredCoupon, new()
        {
            var n = schedule.Count;

            QLNet.Utils.QL_REQUIRE(!nominals.empty(), () => "no notional given");
            QLNet.Utils.QL_REQUIRE(nominals.Count <= n, () => "too many nominals (" + nominals.Count + "), only " + n + " required");
            if (gearings != null)
            {
                QLNet.Utils.QL_REQUIRE(gearings.Count <= n, () => "too many gearings (" + gearings.Count + "), only " + n + " required");
            }

            if (spreads != null)
            {
                QLNet.Utils.QL_REQUIRE(spreads.Count <= n, () => "too many spreads (" + spreads.Count + "), only " + n + " required");
            }

            if (caps != null)
            {
                QLNet.Utils.QL_REQUIRE(caps.Count <= n, () => "too many caps (" + caps.Count + "), only " + n + " required");
            }

            if (floors != null)
            {
                QLNet.Utils.QL_REQUIRE(floors.Count <= n, () => "too many floors (" + floors.Count + "), only " + n + " required");
            }

            QLNet.Utils.QL_REQUIRE(!isZero || !isInArrears, () => "in-arrears and zero features are not compatible");

            var leg = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule.calendar();

            var lastPaymentDate = calendar.adjust(schedule[n - 1], paymentAdj);

            for (var i = 0; i < n - 1; ++i)
            {
                Date refStart, start, refEnd, end;
                refStart = start = schedule[i];
                refEnd = end = schedule[i + 1];
                var paymentDate = isZero ? lastPaymentDate : calendar.adjust(end, paymentAdj);
                if (i == 0 && !schedule.isRegular(i + 1))
                {
                    refStart = calendar.adjust(end - schedule.tenor(), schedule.businessDayConvention());
                }

                if (i == n - 1 && !schedule.isRegular(i + 1))
                {
                    refEnd = calendar.adjust(start + schedule.tenor(), schedule.businessDayConvention());
                }

                if (gearings.Get(i, 1).IsEqual(0.0))
                {
                    // fixed coupon
                    leg.Add(new FixedRateCoupon(paymentDate, nominals.Get(i),
                        QLNet.Utils.effectiveFixedRate(spreads, caps, floors, i),
                        paymentDayCounter,
                        start, end, refStart, refEnd));
                }
                else
                {
                    if (QLNet.Utils.noOption(caps, floors, i))
                    {
                        leg.Add(FastActivator<FloatingCouponType>.Create().factory(
                            nominals.Get(i),
                            paymentDate, start, end,
                            fixingDays.Get(i, index.fixingDays()),
                            index,
                            gearings.Get(i, 1),
                            spreads.Get(i),
                            refStart, refEnd, paymentDayCounter,
                            isInArrears));
                    }
                    else
                    {
                        leg.Add(FastActivator<CappedFlooredCouponType>.Create().Factory(
                            nominals.Get(i),
                            paymentDate, start, end,
                            fixingDays.Get(i, index.fixingDays()),
                            index,
                            gearings.Get(i, 1),
                            spreads.Get(i),
                            Utils.toNullable(caps.Get(i, double.MinValue)),
                            Utils.toNullable(floors.Get(i, double.MinValue)),
                            refStart, refEnd, paymentDayCounter,
                            isInArrears));
                    }
                }
            }

            return leg;
        }

        public static List<CashFlow> OvernightLeg(List<double> nominals,
            Schedule schedule,
            BusinessDayConvention paymentAdjustment,
            OvernightIndex overnightIndex,
            List<double> gearings,
            List<double> spreads,
            DayCounter paymentDayCounter)
        {
            QLNet.Utils.QL_REQUIRE(!nominals.empty(), () => "no nominal given");

            var leg = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            var n = schedule.Count;
            for (var i = 0; i < n - 1; ++i)
            {
                refStart = start = schedule.date(i);
                refEnd = end = schedule.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdjustment);
                if (i == 0 && !schedule.isRegular(i + 1))
                {
                    refStart = calendar.adjust(end - schedule.tenor(), paymentAdjustment);
                }

                if (i == n - 1 && !schedule.isRegular(i + 1))
                {
                    refEnd = calendar.adjust(start + schedule.tenor(), paymentAdjustment);
                }

                leg.Add(new OvernightIndexedCoupon(paymentDate,
                    nominals.Get(i),
                    start, end,
                    overnightIndex,
                    gearings.Get(i, 1.0),
                    spreads.Get(i, 0.0),
                    refStart, refEnd,
                    paymentDayCounter));
            }

            return leg;
        }

        public static List<CashFlow> yoyInflationLeg(List<double> notionals_,
            Schedule schedule_,
            BusinessDayConvention paymentAdjustment_,
            YoYInflationIndex index_,
            List<double> gearings_,
            List<double> spreads_,
            DayCounter paymentDayCounter_,
            List<double?> caps_,
            List<double?> floors_,
            Calendar paymentCalendar_,
            List<int> fixingDays_,
            Period observationLag_)
        {
            var n = schedule_.Count - 1;

            QLNet.Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");
            QLNet.Utils.QL_REQUIRE(notionals_.Count <= n, () => "too many nominals (" + notionals_.Count + "), only " + n + " required");
            if (gearings_ != null)
            {
                QLNet.Utils.QL_REQUIRE(gearings_.Count <= n, () => "too many gearings (" + gearings_.Count + "), only " + n + " required");
            }

            if (spreads_ != null)
            {
                QLNet.Utils.QL_REQUIRE(spreads_.Count <= n, () => "too many spreads (" + spreads_.Count + "), only " + n + " required");
            }

            if (caps_ != null)
            {
                QLNet.Utils.QL_REQUIRE(caps_.Count <= n, () => "too many caps (" + caps_.Count + "), only " + n + " required");
            }

            if (floors_ != null)
            {
                QLNet.Utils.QL_REQUIRE(floors_.Count <= n, () => "too many floors (" + floors_.Count + "), only " + n + " required");
            }

            var leg = new List<CashFlow>(n);

            var calendar = paymentCalendar_;

            Date refStart, start, refEnd, end;

            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule_.date(i);
                refEnd = end = schedule_.date(i + 1);
                var paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
                }

                if (i == n - 1 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
                }

                if (gearings_.Get(i, 1.0).IsEqual(0.0))
                {
                    // fixed coupon
                    leg.Add(new FixedRateCoupon(paymentDate, notionals_.Get(i, 1.0),
                        QLNet.Utils.effectiveFixedRate(spreads_, caps_, floors_, i),
                        paymentDayCounter_,
                        start, end, refStart, refEnd));
                }
                else
                {
                    // yoy inflation coupon
                    if (QLNet.Utils.noOption(caps_, floors_, i))
                    {
                        // just swaplet
                        var coup = new YoYInflationCoupon(paymentDate,
                            notionals_.Get(i, 1.0),
                            start, end,
                            fixingDays_.Get(i, 0),
                            index_,
                            observationLag_,
                            paymentDayCounter_,
                            gearings_.Get(i, 1.0),
                            spreads_.Get(i, 0.0),
                            refStart, refEnd);

                        // in this case you can set a pricer
                        // straight away because it only provides computation - not data
                        var pricer = new YoYInflationCouponPricer();
                        coup.setPricer(pricer);
                        leg.Add(coup);
                    }
                    else
                    {
                        // cap/floorlet
                        leg.Add(new CappedFlooredYoYInflationCoupon(
                            paymentDate,
                            notionals_.Get(i, 1.0),
                            start, end,
                            fixingDays_.Get(i, 0),
                            index_,
                            observationLag_,
                            paymentDayCounter_,
                            gearings_.Get(i, 1.0),
                            spreads_.Get(i, 0.0),
                            Utils.toNullable(caps_.Get(i, double.MinValue)),
                            Utils.toNullable(floors_.Get(i, double.MinValue)),
                            refStart, refEnd));
                    }
                }
            }

            return leg;
        }
    }
}
