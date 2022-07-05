/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! Year-on-year inflation-indexed swap
    /*! Quoted as a fixed rate \f$ K \f$.  At start:
        \f[
        \sum_{i=1}^{M} P_n(0,t_i) N K =
        \sum_{i=1}^{M} P_n(0,t_i) N \left[ \frac{I(t_i)}{I(t_i-1)} - 1 \right]
        \f]
        where \f$ t_M \f$ is the maturity time, \f$ P_n(0,t) \f$ is the
        nominal discount factor at time \f$ t \f$, \f$ N \f$ is the
        notional, and \f$ I(t) \f$ is the inflation index value at
        time \f$ t \f$.

        \note These instruments have now been changed to follow
              typical VanillaSwap ExerciseType design conventions
              w.r.t. Schedules etc.
    */
    [PublicAPI]
    public class YearOnYearInflationSwap : Swap
    {
        public enum Type
        {
            Receiver = -1,
            Payer = 1
        }

        //! %Arguments for YoY swap calculation
        public new class Arguments : Swap.Arguments
        {
            public Arguments()
            {
                type = Type.Receiver;
                nominal = null;
            }

            public List<double> fixedCoupons { get; set; }

            public List<Date> fixedPayDates { get; set; }

            public List<Date> fixedResetDates { get; set; }

            public double? nominal { get; set; }

            public Type type { get; set; }

            public List<double> yoyAccrualTimes { get; set; }

            public List<double?> yoyCoupons { get; set; }

            public List<Date> yoyFixingDates { get; set; }

            public List<Date> yoyPayDates { get; set; }

            public List<Date> yoyResetDates { get; set; }

            public List<double> yoySpreads { get; set; }

            public override void validate()
            {
                base.validate();
                QLNet.Utils.QL_REQUIRE(nominal != null, () => "nominal null or not set");
                QLNet.Utils.QL_REQUIRE(fixedResetDates.Count == fixedPayDates.Count, () =>
                    "number of fixed start dates different from number of fixed payment dates");
                QLNet.Utils.QL_REQUIRE(fixedPayDates.Count == fixedCoupons.Count, () =>
                    "number of fixed payment dates different from number of fixed coupon amounts");
                QLNet.Utils.QL_REQUIRE(yoyResetDates.Count == yoyPayDates.Count, () =>
                    "number of yoy start dates different from number of yoy payment dates");
                QLNet.Utils.QL_REQUIRE(yoyFixingDates.Count == yoyPayDates.Count, () =>
                    "number of yoy fixing dates different from number of yoy payment dates");
                QLNet.Utils.QL_REQUIRE(yoyAccrualTimes.Count == yoyPayDates.Count, () =>
                    "number of yoy accrual Times different from number of yoy payment dates");
                QLNet.Utils.QL_REQUIRE(yoySpreads.Count == yoyPayDates.Count, () =>
                    "number of yoy spreads different from number of yoy payment dates");
                QLNet.Utils.QL_REQUIRE(yoyPayDates.Count == yoyCoupons.Count, () =>
                    "number of yoy payment dates different from number of yoy coupon amounts");
            }
        }

        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        //! %Results from YoY swap calculation
        public new class Results : Swap.Results
        {
            public double? fairRate { get; set; }

            public double? fairSpread { get; set; }

            public override void reset()
            {
                base.reset();
                fairRate = null;
                fairSpread = null;
            }
        }

        // results
        private double? fairRate_;
        private double? fairSpread_;
        private DayCounter fixedDayCount_;
        private double fixedRate_;
        private Schedule fixedSchedule_;
        private double nominal_;
        private Period observationLag_;
        private Calendar paymentCalendar_;
        private BusinessDayConvention paymentConvention_;
        private double spread_;
        private Type type_;
        private DayCounter yoyDayCount_;
        private YoYInflationIndex yoyIndex_;
        private Schedule yoySchedule_;

        public YearOnYearInflationSwap(
            Type type,
            double nominal,
            Schedule fixedSchedule,
            double fixedRate,
            DayCounter fixedDayCount,
            Schedule yoySchedule,
            YoYInflationIndex yoyIndex,
            Period observationLag,
            double spread,
            DayCounter yoyDayCount,
            Calendar paymentCalendar, // inflation index does not have a calendar
            BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing)
            : base(2)
        {
            type_ = type;
            nominal_ = nominal;
            fixedSchedule_ = fixedSchedule;
            fixedRate_ = fixedRate;
            fixedDayCount_ = fixedDayCount;
            yoySchedule_ = yoySchedule;
            yoyIndex_ = yoyIndex;
            observationLag_ = observationLag;
            spread_ = spread;
            yoyDayCount_ = yoyDayCount;
            paymentCalendar_ = paymentCalendar;
            paymentConvention_ = paymentConvention;

            // N.B. fixed leg gets its calendar from the schedule!
            List<CashFlow> fixedLeg = new FixedRateLeg(fixedSchedule_)
                .withCouponRates(fixedRate_, fixedDayCount_) // Simple compounding by default
                .withNotionals(nominal_)
                .withPaymentAdjustment(paymentConvention_);

            List<CashFlow> yoyLeg = new yoyInflationLeg(yoySchedule_, paymentCalendar_, yoyIndex_, observationLag_)
                .withSpreads(spread_)
                .withPaymentDayCounter(yoyDayCount_)
                .withNotionals(nominal_)
                .withPaymentAdjustment(paymentConvention_);

            yoyLeg.ForEach((i, x) => x.registerWith(update));

            legs_[0] = fixedLeg;
            legs_[1] = yoyLeg;
            if (type_ == Type.Payer)
            {
                payer_[0] = -1.0;
                payer_[1] = +1.0;
            }
            else
            {
                payer_[0] = +1.0;
                payer_[1] = -1.0;
            }
        }

        public virtual double fairRate()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(fairRate_ != null, () => "result not available");
            return fairRate_.Value;
        }

        public virtual double fairSpread()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(fairSpread_ != null, () => "result not available");
            return fairSpread_.Value;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            // copy from VanillaSwap
            // works because similarly simple instrument
            // that we always expect to be priced with a swap engine

            base.fetchResults(r);

            if (r is Results results)
            {
                // might be a swap engine, so no error is thrown
                fairRate_ = results.fairRate;
                fairSpread_ = results.fairSpread;
            }
            else
            {
                fairRate_ = null;
                fairSpread_ = null;
            }

            if (fairRate_ == null)
            {
                // calculate it from other results
                if (legBPS_[0] != null)
                {
                    fairRate_ = fixedRate_ - NPV_ / (legBPS_[0] / Const.BASIS_POINT);
                }
            }

            if (fairSpread_ == null)
            {
                // ditto
                if (legBPS_[1] != null)
                {
                    fairSpread_ = spread_ - NPV_ / (legBPS_[1] / Const.BASIS_POINT);
                }
            }
        }

        public virtual DayCounter fixedDayCount() => fixedDayCount_;

        public virtual List<CashFlow> fixedLeg() => legs_[0];

        // results
        public virtual double fixedLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
            return legNPV_[0].Value;
        }

        public virtual double fixedRate() => fixedRate_;

        public virtual Schedule fixedSchedule() => fixedSchedule_;

        public virtual double nominal() => nominal_;

        public virtual Period observationLag() => observationLag_;

        public virtual Calendar paymentCalendar() => paymentCalendar_;

        public virtual BusinessDayConvention paymentConvention() => paymentConvention_;

        // other
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            if (!(args is Arguments arguments)) // it's a swap engine...
            {
                return;
            }

            arguments.type = type_;
            arguments.nominal = nominal_;

            var fixedCoupons = fixedLeg();

            arguments.fixedResetDates = arguments.fixedPayDates = new List<Date>(fixedCoupons.Count);
            arguments.fixedCoupons = new List<double>(fixedCoupons.Count);

            for (var i = 0; i < fixedCoupons.Count; ++i)
            {
                var coupon = fixedCoupons[i] as FixedRateCoupon;

                arguments.fixedPayDates.Add(coupon.date());
                arguments.fixedResetDates.Add(coupon.accrualStartDate());
                arguments.fixedCoupons.Add(coupon.amount());
            }

            var yoyCoupons = yoyLeg();

            arguments.yoyResetDates = arguments.yoyPayDates = arguments.yoyFixingDates = new List<Date>(yoyCoupons.Count);
            arguments.yoyAccrualTimes = new List<double>(yoyCoupons.Count);
            arguments.yoySpreads = new List<double>(yoyCoupons.Count);
            arguments.yoyCoupons = new List<double?>(yoyCoupons.Count);
            for (var i = 0; i < yoyCoupons.Count; ++i)
            {
                var coupon = yoyCoupons[i] as YoYInflationCoupon;

                arguments.yoyResetDates.Add(coupon.accrualStartDate());
                arguments.yoyPayDates.Add(coupon.date());

                arguments.yoyFixingDates.Add(coupon.fixingDate());
                arguments.yoyAccrualTimes.Add(coupon.accrualPeriod());
                arguments.yoySpreads.Add(coupon.spread());
                try
                {
                    arguments.yoyCoupons.Add(coupon.amount());
                }
                catch (Exception)
                {
                    arguments.yoyCoupons.Add(null);
                }
            }
        }

        public virtual double spread() => spread_;

        // inspectors
        public virtual Type type() => type_;

        public virtual DayCounter yoyDayCount() => yoyDayCount_;

        public virtual YoYInflationIndex yoyInflationIndex() => yoyIndex_;

        public virtual List<CashFlow> yoyLeg() => legs_[1];

        public virtual double yoyLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
            return legNPV_[1].Value;
        }

        public virtual Schedule yoySchedule() => yoySchedule_;

        protected override void setupExpired()
        {
            base.setupExpired();
            legBPS_[0] = legBPS_[1] = 0.0;
            fairRate_ = null;
            fairSpread_ = null;
        }
    }
}
