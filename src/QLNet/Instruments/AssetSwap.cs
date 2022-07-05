/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! Bullet bond vs %Libor swap
    /*! for mechanics of par asset swap and market asset swap, refer to
        "Introduction to Asset Swap", Lehman Brothers European Fixed
        Income Research - January 2000, D. O'Kane

        \ingroup instruments

        \warning bondCleanPrice must be the (forward) price at the
                 floatSchedule start date

        \bug fair prices are not calculated correctly when using
             indexed coupons.
    */
    [PublicAPI]
    public class AssetSwap : Swap
    {
        //! %Arguments for asset swap calculation
        public new class Arguments : Swap.Arguments
        {
            public List<double> fixedCoupons { get; set; }

            public List<Date> fixedPayDates { get; set; }

            public List<Date> fixedResetDates { get; set; }

            public List<double> floatingAccrualTimes { get; set; }

            public List<Date> floatingFixingDates { get; set; }

            public List<Date> floatingPayDates { get; set; }

            public List<Date> floatingResetDates { get; set; }

            public List<double> floatingSpreads { get; set; }

            public override void validate()
            {
                QLNet.Utils.QL_REQUIRE(fixedResetDates.Count == fixedPayDates.Count, () =>
                    "number of fixed start dates different from " +
                    "number of fixed payment dates");
                QLNet.Utils.QL_REQUIRE(fixedPayDates.Count == fixedCoupons.Count, () =>
                    "number of fixed payment dates different from " +
                    "number of fixed coupon amounts");
                QLNet.Utils.QL_REQUIRE(floatingResetDates.Count == floatingPayDates.Count, () =>
                    "number of floating start dates different from " +
                    "number of floating payment dates");
                QLNet.Utils.QL_REQUIRE(floatingFixingDates.Count == floatingPayDates.Count, () =>
                    "number of floating fixing dates different from " +
                    "number of floating payment dates");
                QLNet.Utils.QL_REQUIRE(floatingAccrualTimes.Count == floatingPayDates.Count, () =>
                    "number of floating accrual times different from " +
                    "number of floating payment dates");
                QLNet.Utils.QL_REQUIRE(floatingSpreads.Count == floatingPayDates.Count, () =>
                    "number of floating spreads different from " +
                    "number of floating payment dates");
            }
        }

        //! %Results from simple swap calculation
        public new class Results : Swap.Results
        {
            public double? fairCleanPrice { get; set; }

            public double? fairNonParRepayment { get; set; }

            public double? fairSpread { get; set; }

            public override void reset()
            {
                base.reset();
                fairSpread = null;
                fairCleanPrice = null;
                fairNonParRepayment = null;
            }
        }

        private Bond bond_;
        private double bondCleanPrice_, nonParRepayment_;
        private double? fairCleanPrice_, fairNonParRepayment_;
        // results
        private double? fairSpread_;
        private bool parSwap_;
        private double spread_;
        private Date upfrontDate_;

        public AssetSwap(bool payBondCoupon,
            Bond bond,
            double bondCleanPrice,
            IborIndex iborIndex,
            double spread,
            Schedule floatSchedule = null,
            DayCounter floatingDayCount = null,
            bool parAssetSwap = true)
            : base(2)
        {
            bond_ = bond;
            bondCleanPrice_ = bondCleanPrice;
            nonParRepayment_ = 100;
            spread_ = spread;
            parSwap_ = parAssetSwap;

            var schedule = floatSchedule;
            if (floatSchedule == null)
            {
                schedule = new Schedule(bond_.settlementDate(),
                    bond_.maturityDate(),
                    iborIndex.tenor(),
                    iborIndex.fixingCalendar(),
                    iborIndex.businessDayConvention(),
                    iborIndex.businessDayConvention(),
                    DateGeneration.Rule.Backward,
                    false); // endOfMonth
            }

            // the following might become an input parameter
            var paymentAdjustment = BusinessDayConvention.Following;

            var finalDate = schedule.calendar().adjust(schedule.endDate(), paymentAdjustment);
            var adjBondMaturityDate = schedule.calendar().adjust(bond_.maturityDate(), paymentAdjustment);

            QLNet.Utils.QL_REQUIRE(finalDate == adjBondMaturityDate, () =>
                "adjusted schedule end date (" +
                finalDate +
                ") must be equal to adjusted bond maturity date (" +
                adjBondMaturityDate + ")");

            // bondCleanPrice must be the (forward) clean price
            // at the floating schedule start date
            upfrontDate_ = schedule.startDate();
            var dirtyPrice = bondCleanPrice_ +
                             bond_.accruedAmount(upfrontDate_);

            var notional = bond_.notional(upfrontDate_);
            /* In the market asset swap, the bond is purchased in return for
               payment of the full price. The notional of the floating leg is
               then scaled by the full price. */
            if (!parSwap_)
            {
                notional *= dirtyPrice / 100.0;
            }

            if (floatingDayCount == null)
            {
                legs_[1] = new IborLeg(schedule, iborIndex)
                    .withSpreads(spread)
                    .withNotionals(notional)
                    .withPaymentAdjustment(paymentAdjustment);
            }
            else
            {
                legs_[1] = new IborLeg(schedule, iborIndex)
                    .withSpreads(spread)
                    .withPaymentDayCounter(floatingDayCount)
                    .withNotionals(notional)
                    .withPaymentAdjustment(paymentAdjustment);
            }

            foreach (var c in legs_[1])
            {
                c.registerWith(update);
            }

            var bondLeg = bond_.cashflows();
            foreach (var c in bondLeg)
            {
                // whatever might be the choice for the discounting engine
                // bond flows on upfrontDate_ must be discarded
                var upfrontDateBondFlows = false;
                if (!c.hasOccurred(upfrontDate_, upfrontDateBondFlows))
                {
                    legs_[0].Add(c);
                }
            }

            QLNet.Utils.QL_REQUIRE(!legs_[0].empty(), () => "empty bond leg to start with");

            // special flows
            if (parSwap_)
            {
                // upfront on the floating leg
                var upfront = (dirtyPrice - 100.0) / 100.0 * notional;
                CashFlow upfrontCashFlow = new SimpleCashFlow(upfront, upfrontDate_);
                legs_[1].Insert(0, upfrontCashFlow);
                // backpayment on the floating leg
                // (accounts for non-par redemption, if any)
                var backPayment = notional;
                CashFlow backPaymentCashFlow = new SimpleCashFlow(backPayment, finalDate);
                legs_[1].Add(backPaymentCashFlow);
            }
            else
            {
                // final notional exchange
                CashFlow finalCashFlow = new SimpleCashFlow(notional, finalDate);
                legs_[1].Add(finalCashFlow);
            }

            QLNet.Utils.QL_REQUIRE(!legs_[0].empty(), () => "empty bond leg");

            foreach (var c in legs_[0])
            {
                c.registerWith(update);
            }

            if (payBondCoupon)
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

        public AssetSwap(bool parAssetSwap,
            Bond bond,
            double bondCleanPrice,
            double nonParRepayment,
            double gearing,
            IborIndex iborIndex,
            double spread = 0.0,
            DayCounter floatingDayCount = null,
            Date dealMaturity = null,
            bool payBondCoupon = false)
            : base(2)
        {
            bond_ = bond;
            bondCleanPrice_ = bondCleanPrice;
            nonParRepayment_ = nonParRepayment;
            spread_ = spread;
            parSwap_ = parAssetSwap;

            var tempSch = new Schedule(bond_.settlementDate(),
                bond_.maturityDate(),
                iborIndex.tenor(),
                iborIndex.fixingCalendar(),
                iborIndex.businessDayConvention(),
                iborIndex.businessDayConvention(),
                DateGeneration.Rule.Backward,
                false); // endOfMonth

            if (dealMaturity == null)
            {
                dealMaturity = bond_.maturityDate();
            }

            QLNet.Utils.QL_REQUIRE(dealMaturity <= tempSch.dates().Last(), () =>
                "deal maturity " + dealMaturity +
                " cannot be later than (adjusted) bond maturity " +
                tempSch.dates().Last());
            QLNet.Utils.QL_REQUIRE(dealMaturity > tempSch.dates()[0], () =>
                "deal maturity " + dealMaturity +
                " must be later than swap start date " +
                tempSch.dates()[0]);

            // the following might become an input parameter
            var paymentAdjustment = BusinessDayConvention.Following;

            var finalDate = tempSch.calendar().adjust(dealMaturity, paymentAdjustment);
            var schedule = tempSch.until(finalDate);

            // bondCleanPrice must be the (forward) clean price
            // at the floating schedule start date
            upfrontDate_ = schedule.startDate();
            var dirtyPrice = bondCleanPrice_ +
                             bond_.accruedAmount(upfrontDate_);

            var notional = bond_.notional(upfrontDate_);
            /* In the market asset swap, the bond is purchased in return for
               payment of the full price. The notional of the floating leg is
               then scaled by the full price. */
            if (!parSwap_)
            {
                notional *= dirtyPrice / 100.0;
            }

            if (floatingDayCount == null)
            {
                legs_[1] = new IborLeg(schedule, iborIndex)
                    .withSpreads(spread)
                    .withGearings(gearing)
                    .withNotionals(notional)
                    .withPaymentAdjustment(paymentAdjustment);
            }
            else
            {
                legs_[1] = new IborLeg(schedule, iborIndex)
                    .withSpreads(spread)
                    .withGearings(gearing)
                    .withPaymentDayCounter(floatingDayCount)
                    .withNotionals(notional)
                    .withPaymentAdjustment(paymentAdjustment);
            }

            foreach (var c in legs_[1])
            {
                c.registerWith(update);
            }

            var bondLeg = bond_.cashflows();
            // skip bond redemption
            int i;
            for (i = 0; i < bondLeg.Count && bondLeg[i].date() <= dealMaturity; ++i)
            {
                // whatever might be the choice for the discounting engine
                // bond flows on upfrontDate_ must be discarded
                var upfrontDateBondFlows = false;
                if (!bondLeg[i].hasOccurred(upfrontDate_, upfrontDateBondFlows))
                {
                    legs_[0].Add(bondLeg[i]);
                }
            }

            // if the first skipped cashflow is not the redemption
            // and it is a coupon then add the accrued coupon
            if (i < bondLeg.Count - 1)
            {
                var c = bondLeg[i] as Coupon;
                if (c != null)
                {
                    CashFlow accruedCoupon = new SimpleCashFlow(c.accruedAmount(dealMaturity), finalDate);
                    legs_[0].Add(accruedCoupon);
                }
            }

            // add the nonParRepayment_
            CashFlow nonParRepaymentFlow = new SimpleCashFlow(nonParRepayment_, finalDate);
            legs_[0].Add(nonParRepaymentFlow);

            QLNet.Utils.QL_REQUIRE(!legs_[0].empty(), () => "empty bond leg to start with");

            // special flows
            if (parSwap_)
            {
                // upfront on the floating leg
                var upfront = (dirtyPrice - 100.0) / 100.0 * notional;
                CashFlow upfrontCashFlow = new SimpleCashFlow(upfront, upfrontDate_);
                legs_[1].Insert(0, upfrontCashFlow);
                // backpayment on the floating leg
                // (accounts for non-par redemption, if any)
                var backPayment = notional;
                CashFlow backPaymentCashFlow = new SimpleCashFlow(backPayment, finalDate);
                legs_[1].Add(backPaymentCashFlow);
            }
            else
            {
                // final notional exchange
                CashFlow finalCashFlow = new SimpleCashFlow(notional, finalDate);
                legs_[1].Add(finalCashFlow);
            }

            QLNet.Utils.QL_REQUIRE(!legs_[0].empty(), () => "empty bond leg");

            foreach (var c in legs_[0])
            {
                c.registerWith(update);
            }

            if (payBondCoupon)
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

        public Bond bond() => bond_;

        public List<CashFlow> bondLeg() => legs_[0];

        public double cleanPrice() => bondCleanPrice_;

        public double fairCleanPrice()
        {
            calculate();
            if (fairCleanPrice_ != null)
            {
                return fairCleanPrice_.Value;
            }

            QLNet.Utils.QL_REQUIRE(startDiscounts_[1] != null, () => "fair clean price not available for seasoned deal");
            var notional = bond_.notional(upfrontDate_);
            if (parSwap_)
            {
                fairCleanPrice_ = bondCleanPrice_ - payer_[1] *
                    NPV_ * npvDateDiscount_ / startDiscounts_[1] / (notional / 100.0);
            }
            else
            {
                var accruedAmount = bond_.accruedAmount(upfrontDate_);
                var dirtyPrice = bondCleanPrice_ + accruedAmount;
                var fairDirtyPrice = -legNPV_[0].Value / legNPV_[1].Value * dirtyPrice;
                fairCleanPrice_ = fairDirtyPrice - accruedAmount;
            }

            return fairCleanPrice_.Value;
        }

        public double fairNonParRepayment()
        {
            calculate();
            if (fairNonParRepayment_ != null)
            {
                return fairNonParRepayment_.Value;
            }

            QLNet.Utils.QL_REQUIRE(endDiscounts_[1] != null, () => "fair non par repayment not available for expired leg");
            var notional = bond_.notional(upfrontDate_);
            fairNonParRepayment_ = nonParRepayment_ - payer_[0] *
                NPV_ * npvDateDiscount_ / endDiscounts_[1] / (notional / 100.0);
            return fairNonParRepayment_.Value;
        }

        // results
        public double fairSpread()
        {
            calculate();
            if (fairSpread_ != null)
            {
                return fairSpread_.Value;
            }

            if (legBPS_.Count > 1 && legBPS_[1] != null)
            {
                fairSpread_ = spread_ - NPV_ / legBPS_[1] * Const.BASIS_POINT;
                return fairSpread_.Value;
            }

            QLNet.Utils.QL_FAIL("fair spread not available");
            return 0;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);
            if (r is Results results)
            {
                fairSpread_ = results.fairSpread;
                fairCleanPrice_ = results.fairCleanPrice;
                fairNonParRepayment_ = results.fairNonParRepayment;
            }
            else
            {
                fairSpread_ = null;
                fairCleanPrice_ = null;
                fairNonParRepayment_ = null;
            }
        }

        public List<CashFlow> floatingLeg() => legs_[1];

        public double floatingLegBPS()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legBPS_.Count > 1 && legBPS_[1] != null, () => "floating-leg BPS not available");
            return legBPS_[1].GetValueOrDefault();
        }

        public double floatingLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_.Count > 1 && legNPV_[1] != null, () => "floating-leg NPV not available");
            return legNPV_[1].GetValueOrDefault();
        }

        public double nonParRepayment() => nonParRepayment_;

        // inspectors
        public bool parSwap() => parSwap_;

        public bool payBondCoupon() => payer_[0].IsEqual(-1.0);

        // other
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            if (!(args is Arguments arguments)) // it's a swap engine...
            {
                return;
            }

            var fixedCoupons = bondLeg();

            arguments.fixedResetDates = arguments.fixedPayDates = new List<Date>(fixedCoupons.Count);
            arguments.fixedCoupons = new List<double>(fixedCoupons.Count);

            for (var i = 0; i < fixedCoupons.Count; ++i)
            {
                var coupon = fixedCoupons[i] as FixedRateCoupon;

                arguments.fixedPayDates[i] = coupon.date();
                arguments.fixedResetDates[i] = coupon.accrualStartDate();
                arguments.fixedCoupons[i] = coupon.amount();
            }

            var floatingCoupons = floatingLeg();

            arguments.floatingResetDates = arguments.floatingPayDates =
                arguments.floatingFixingDates = new List<Date>(floatingCoupons.Count);
            arguments.floatingAccrualTimes = new List<double>(floatingCoupons.Count);
            arguments.floatingSpreads = new List<double>(floatingCoupons.Count);

            for (var i = 0; i < floatingCoupons.Count; ++i)
            {
                var coupon = floatingCoupons[i] as FloatingRateCoupon;

                arguments.floatingResetDates[i] = coupon.accrualStartDate();
                arguments.floatingPayDates[i] = coupon.date();
                arguments.floatingFixingDates[i] = coupon.fixingDate();
                arguments.floatingAccrualTimes[i] = coupon.accrualPeriod();
                arguments.floatingSpreads[i] = coupon.spread();
            }
        }

        public double spread() => spread_;

        protected override void setupExpired()
        {
            base.setupExpired();
            fairSpread_ = null;
            fairCleanPrice_ = null;
            fairNonParRepayment_ = null;
        }
    }
}
