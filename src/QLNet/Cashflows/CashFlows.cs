/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
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

using System.Linq;
using JetBrains.Annotations;
using QLNet.Exceptions;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Patterns;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Yield;
using QLNet.Time;
using Leg = System.Collections.Generic.List<QLNet.CashFlow>;

namespace QLNet.Cashflows
{
    //! %cashflow-analysis functions
    [PublicAPI]
    public class CashFlows
    {
        #region utility functions

        private static double aggregateRate(Leg leg, CashFlow cf)
        {
            if (cf == null)
            {
                return 0.0;
            }

            var paymentDate = cf.date();
            var firstCouponFound = false;
            var nominal = 0.0;
            var accrualPeriod = 0.0;
            DayCounter dc = null;
            var result = 0.0;

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    if (firstCouponFound)
                    {
                        QLNet.Utils.QL_REQUIRE(nominal.IsEqual(cp.nominal()) &&
                                                        accrualPeriod.IsEqual(cp.accrualPeriod()) &&
                                                        dc == cp.dayCounter(), () =>
                            "cannot aggregate two different coupons on "
                            + paymentDate);
                    }
                    else
                    {
                        firstCouponFound = true;
                        nominal = cp.nominal();
                        accrualPeriod = cp.accrualPeriod();
                        dc = cp.dayCounter();
                    }

                    result += cp.rate();
                }
            }

            QLNet.Utils.QL_REQUIRE(firstCouponFound, () => "no coupon paid at cashflow date " + paymentDate);
            return result;
        }

        public static double simpleDuration(Leg leg, InterestRate y, bool includeSettlementDateFlows,
            Date settlementDate, Date npvDate)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var P = 0.0;
            var dPdy = 0.0;
            var t = 0.0;
            var lastDate = npvDate;

            var dc = y.dayCounter();
            for (var i = 0; i < leg.Count; ++i)
            {
                if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
                {
                    continue;
                }

                var c = leg[i].amount();
                if (leg[i].tradingExCoupon(settlementDate))
                {
                    c = 0.0;
                }

                t += getStepwiseDiscountTime(leg[i], dc, npvDate, lastDate);
                var B = y.discountFactor(t);
                P += c * B;
                dPdy += t * c * B;

                lastDate = leg[i].date();
            }

            if (P.IsEqual(0.0)) // no cashflows
            {
                return 0.0;
            }

            return dPdy / P;
        }

        public static double modifiedDuration(Leg leg, InterestRate y, bool includeSettlementDateFlows,
            Date settlementDate, Date npvDate)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var P = 0.0;
            var t = 0.0;
            var dPdy = 0.0;
            var r = y.rate();
            var N = (int)y.frequency();
            var lastDate = npvDate;
            var dc = y.dayCounter();

            for (var i = 0; i < leg.Count; ++i)
            {
                if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
                {
                    continue;
                }

                var c = leg[i].amount();
                if (leg[i].tradingExCoupon(settlementDate))
                {
                    c = 0.0;
                }

                t += getStepwiseDiscountTime(leg[i], dc, npvDate, lastDate);

                var B = y.discountFactor(t);
                P += c * B;
                switch (y.compounding())
                {
                    case Compounding.Simple:
                        dPdy -= c * B * B * t;
                        break;
                    case Compounding.Compounded:
                        dPdy -= c * t * B / (1 + r / N);
                        break;
                    case Compounding.Continuous:
                        dPdy -= c * B * t;
                        break;
                    case Compounding.SimpleThenCompounded:
                        if (t <= 1.0 / N)
                        {
                            dPdy -= c * B * B * t;
                        }
                        else
                        {
                            dPdy -= c * t * B / (1 + r / N);
                        }

                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown compounding convention (" + y.compounding() + ")");
                        break;
                }

                lastDate = leg[i].date();
            }

            if (P.IsEqual(0.0)) // no cashflows
            {
                return 0.0;
            }

            return -dPdy / P; // reverse derivative sign
        }

        public static double macaulayDuration(Leg leg, InterestRate y, bool includeSettlementDateFlows,
            Date settlementDate, Date npvDate)
        {
            QLNet.Utils.QL_REQUIRE(y.compounding() == Compounding.Compounded, () => "compounded rate required");

            return (1.0 + y.rate() / (int)y.frequency()) *
                   modifiedDuration(leg, y, includeSettlementDateFlows, settlementDate, npvDate);
        }

        // helper function used to calculate Time-To-Discount for each stage when calculating discount factor stepwisely
        public static double getStepwiseDiscountTime(CashFlow cashFlow, DayCounter dc, Date npvDate, Date lastDate)
        {
            var cashFlowDate = cashFlow.date();
            Date refStartDate, refEndDate;
            var coupon = cashFlow as Coupon;
            if (coupon != null)
            {
                refStartDate = coupon.referencePeriodStart;
                refEndDate = coupon.referencePeriodEnd;
            }
            else
            {
                if (lastDate == npvDate)
                {
                    // we don't have a previous coupon date,
                    // so we fake it
                    refStartDate = cashFlowDate - new Period(1, TimeUnit.Years);
                }
                else
                {
                    refStartDate = lastDate;
                }

                refEndDate = cashFlowDate;
            }

            if (coupon != null && lastDate != coupon.accrualStartDate())
            {
                var couponPeriod = dc.yearFraction(coupon.accrualStartDate(), cashFlowDate, refStartDate, refEndDate);
                var accruedPeriod = dc.yearFraction(coupon.accrualStartDate(), lastDate, refStartDate, refEndDate);
                return couponPeriod - accruedPeriod;
            }

            return dc.yearFraction(lastDate, cashFlowDate, refStartDate, refEndDate);
        }

        #endregion

        #region Helper Classes

        private class IrrFinder : ISolver1d
        {
            private readonly Compounding compounding_;
            private readonly DayCounter dayCounter_;
            private readonly Frequency frequency_;
            private readonly bool includeSettlementDateFlows_;
            private readonly Leg leg_;
            private readonly double npv_;
            private readonly Date npvDate_;
            private readonly Date settlementDate_;

            public IrrFinder(Leg leg, double npv, DayCounter dayCounter, Compounding comp, Frequency freq,
                bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
            {
                leg_ = leg;
                npv_ = npv;
                dayCounter_ = dayCounter;
                compounding_ = comp;
                frequency_ = freq;
                includeSettlementDateFlows_ = includeSettlementDateFlows;
                settlementDate_ = settlementDate;
                npvDate_ = npvDate;

                if (settlementDate == null)
                {
                    settlementDate_ = Settings.evaluationDate();
                }

                if (npvDate == null)
                {
                    npvDate_ = settlementDate_;
                }

                checkSign();
            }

            public override double derivative(double y)
            {
                var yield = new InterestRate(y, dayCounter_, compounding_, frequency_);
                return modifiedDuration(leg_, yield, includeSettlementDateFlows_, settlementDate_, npvDate_);
            }

            public override double value(double y)
            {
                var yield = new InterestRate(y, dayCounter_, compounding_, frequency_);
                var NPV = npv(leg_, yield, includeSettlementDateFlows_, settlementDate_, npvDate_);
                return npv_ - NPV;
            }

            private void checkSign()
            {
                // depending on the sign of the market price, check that cash
                // flows of the opposite sign have been specified (otherwise
                // IRR is nonsensical.)

                int lastSign = System.Math.Sign(-npv_), signChanges = 0;
                for (var i = 0; i < leg_.Count; ++i)
                {
                    if (!leg_[i].hasOccurred(settlementDate_, includeSettlementDateFlows_) &&
                        !leg_[i].tradingExCoupon(settlementDate_))
                    {
                        var thisSign = System.Math.Sign(leg_[i].amount());
                        if (lastSign * thisSign < 0) // sign change
                        {
                            signChanges++;
                        }

                        if (thisSign != 0)
                        {
                            lastSign = thisSign;
                        }
                    }
                }

                QLNet.Utils.QL_REQUIRE(signChanges > 0, () =>
                    "the given cash flows cannot result in the given market " +
                    "price due to their sign", QLNetExceptionEnum.InvalidPriceSignException);
            }
        }

        private class ZSpreadFinder : ISolver1d
        {
            private readonly ZeroSpreadedTermStructure curve_;
            private readonly bool includeSettlementDateFlows_;
            private readonly Leg leg_;
            private readonly double npv_;
            private readonly Date npvDate_;
            private readonly Date settlementDate_;
            private readonly SimpleQuote zSpread_;

            public ZSpreadFinder(Leg leg, YieldTermStructure discountCurve, double npv, DayCounter dc, Compounding comp, Frequency freq,
                bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
            {
                leg_ = leg;
                npv_ = npv;
                zSpread_ = new SimpleQuote(0.0);
                curve_ = new ZeroSpreadedTermStructure(new Handle<YieldTermStructure>(discountCurve),
                    new Handle<Quote>(zSpread_), comp, freq, dc);
                includeSettlementDateFlows_ = includeSettlementDateFlows;
                settlementDate_ = settlementDate;
                npvDate_ = npvDate;

                if (settlementDate == null)
                {
                    settlementDate_ = Settings.evaluationDate();
                }

                if (npvDate == null)
                {
                    npvDate_ = settlementDate_;
                }

                // if the discount curve allows extrapolation, let's
                // the spreaded curve do too.
                curve_.enableExtrapolation(discountCurve.allowsExtrapolation());
            }

            public override double value(double zSpread)
            {
                zSpread_.setValue(zSpread);
                var NPV = npv(leg_, curve_, includeSettlementDateFlows_, settlementDate_, npvDate_);
                return npv_ - NPV;
            }
        }

        private class BPSCalculator : IAcyclicVisitor
        {
            private readonly YieldTermStructure discountCurve_;
            private double bps_, nonSensNPV_;

            public BPSCalculator(YieldTermStructure discountCurve)
            {
                discountCurve_ = discountCurve;
                nonSensNPV_ = 0.0;
                bps_ = 0.0;
            }

            public double bps() => bps_;

            public double nonSensNPV() => nonSensNPV_;

            #region IAcyclicVisitor pattern

            // visitor classes should implement the generic visit method in the following form
            public void visit(object o)
            {
                var types = new[] { o.GetType() };
                var methodInfo = QLNet.Utils.GetMethodInfo(this, "visit", types);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(this, new[] { o });
                }
            }

            public void visit(Coupon c)
            {
                var bps = c.nominal() *
                          c.accrualPeriod() *
                          discountCurve_.discount(c.date());
                bps_ += bps;
            }

            public void visit(CashFlow cf)
            {
                nonSensNPV_ += cf.amount() * discountCurve_.discount(cf.date());
            }

            #endregion
        }

        #endregion

        #region Date functions

        public static Date startDate(Leg leg)
        {
            QLNet.Utils.QL_REQUIRE(!leg.empty(), () => "empty leg");
            var d = Date.maxDate();
            for (var i = 0; i < leg.Count; ++i)
            {
                var c = leg[i] as Coupon;
                if (c != null)
                {
                    d = Date.Min(d, c.accrualStartDate());
                }
                else
                {
                    d = Date.Min(d, leg[i].date());
                }
            }

            return d;
        }

        public static Date maturityDate(Leg leg)
        {
            QLNet.Utils.QL_REQUIRE(!leg.empty(), () => "empty leg");
            var d = Date.minDate();
            for (var i = 0; i < leg.Count; ++i)
            {
                var c = leg[i] as Coupon;
                if (c != null)
                {
                    d = Date.Max(d, c.accrualEndDate());
                }
                else
                {
                    d = Date.Max(d, leg[i].date());
                }
            }

            return d;
        }

        public static bool isExpired(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (leg.empty())
            {
                return true;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            for (var i = leg.Count; i > 0; --i)
            {
                if (!leg[i - 1].hasOccurred(settlementDate, includeSettlementDateFlows))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region CashFlow functions

        //! the last cashflow paying before or at the given date
        public static CashFlow previousCashFlow(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (leg.empty())
            {
                return null;
            }

            var d = settlementDate ?? Settings.evaluationDate();
            return leg.LastOrDefault(x => x.hasOccurred(d, includeSettlementDateFlows));
        }

        //! the first cashflow paying after the given date
        public static CashFlow nextCashFlow(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (leg.empty())
            {
                return null;
            }

            var d = settlementDate ?? Settings.evaluationDate();

            // the first coupon paying after d is the one we're after
            return leg.FirstOrDefault(x => !x.hasOccurred(d, includeSettlementDateFlows));
        }

        public static Date previousCashFlowDate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

            if (cf == null)
            {
                return null;
            }

            return cf.date();
        }

        public static Date nextCashFlowDate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return null;
            }

            return cf.date();
        }

        public static double? previousCashFlowAmount(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();
            double? result = 0.0;
            result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
            return result;
        }

        public static double? nextCashFlowAmount(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);

            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();
            var result = 0.0;
            result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
            return result;
        }

        #endregion

        #region Coupon inspectors

        public static double previousCouponRate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);
            return aggregateRate(leg, cf);
        }

        public static double nextCouponRate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            return aggregateRate(leg, cf);
        }

        public static double nominal(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0.0;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.nominal();
                }
            }

            return 0.0;
        }

        public static Date accrualStartDate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accrualStartDate();
                }
            }

            return null;
        }

        public static Date accrualEndDate(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accrualEndDate();
                }
            }

            return null;
        }

        public static Date referencePeriodStart(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.referencePeriodStart;
                }
            }

            return null;
        }

        public static Date referencePeriodEnd(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return null;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.referencePeriodEnd;
                }
            }

            return null;
        }

        public static double accrualPeriod(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accrualPeriod();
                }
            }

            return 0;
        }

        public static int accrualDays(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accrualDays();
                }
            }

            return 0;
        }

        public static double accruedPeriod(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0;
            }

            var paymentDate = cf.date();
            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accruedPeriod(settlementDate);
                }
            }

            return 0;
        }

        public static (int accruedDays, double accruedAmount) accruedDaysAndAmount(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return (0, 0);
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return (cp.accruedDays(settlementDate), cp.accruedAmount(settlementDate));
                }
            }

            return (0, 0);
        }

        public static int accruedDays(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0;
            }

            var paymentDate = cf.date();

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    return cp.accruedDays(settlementDate);
                }
            }

            return 0;
        }

        public static double accruedAmount(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            var cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
            if (cf == null)
            {
                return 0;
            }

            var paymentDate = cf.date();
            var result = 0.0;

            foreach (var x in leg.Where(x => x.date() == paymentDate))
            {
                var cp = x as Coupon;
                if (cp != null)
                {
                    result += cp.accruedAmount(settlementDate);
                }
            }

            return result;
        }

        #endregion

        #region YieldTermStructure functions

        //! NPV of the cash flows. The NPV is the sum of the cash flows, each discounted according to the given term structure.
        public static double npv(Leg leg, YieldTermStructure discountCurve, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var totalNPV = 0.0;
            for (var i = 0; i < leg.Count; ++i)
            {
                if (!leg[i].hasOccurred(settlementDate, includeSettlementDateFlows) && !leg[i].tradingExCoupon(settlementDate))
                {
                    totalNPV += leg[i].amount() * discountCurve.discount(leg[i].date());
                }
            }

            return totalNPV / discountCurve.discount(npvDate);
        }

        // Basis-point sensitivity of the cash flows.
        // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according to the given term structure.
        public static double bps(Leg leg, YieldTermStructure discountCurve, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var calc = new BPSCalculator(discountCurve);
            for (var i = 0; i < leg.Count; ++i)
            {
                if (!leg[i].hasOccurred(settlementDate, includeSettlementDateFlows) &&
                    !leg[i].tradingExCoupon(settlementDate))
                {
                    leg[i].accept(calc);
                }
            }

            return Const.BASIS_POINT * calc.bps() / discountCurve.discount(npvDate);
        }

        //! NPV and BPS of the cash flows.
        // The NPV and BPS of the cash flows calculated together for performance reason
        public static void npvbps(Leg leg, YieldTermStructure discountCurve, bool includeSettlementDateFlows,
            Date settlementDate, Date npvDate, out double npv, out double bps)
        {
            npv = bps = 0.0;
            if (leg.empty())
            {
                bps = 0.0;
                return;
            }

            for (var i = 0; i < leg.Count; ++i)
            {
                var cf = leg[i];
                if (!cf.hasOccurred(settlementDate, includeSettlementDateFlows) &&
                    !cf.tradingExCoupon(settlementDate))
                {
                    var cp = leg[i] as Coupon;
                    var df = discountCurve.discount(cf.date());
                    npv += cf.amount() * df;
                    if (cp != null)
                    {
                        bps += cp.nominal() * cp.accrualPeriod() * df;
                    }
                }
            }

            var d = discountCurve.discount(npvDate);
            npv /= d;
            bps = Const.BASIS_POINT * bps / d;
        }

        // At-the-money rate of the cash flows.
        // The result is the fixed rate for which a fixed rate cash flow  vector, equivalent to the input vector, has the required NPV according to the given term structure. If the required NPV is
        //  not given, the input cash flow vector's NPV is used instead.
        public static double atmRate(Leg leg, YieldTermStructure discountCurve, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null, double? targetNpv = null)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var npv = 0.0;
            var calc = new BPSCalculator(discountCurve);
            for (var i = 0; i < leg.Count; ++i)
            {
                var cf = leg[i];
                if (!cf.hasOccurred(settlementDate, includeSettlementDateFlows) &&
                    !cf.tradingExCoupon(settlementDate))
                {
                    npv += cf.amount() * discountCurve.discount(cf.date());
                    cf.accept(calc);
                }
            }

            if (targetNpv == null)
            {
                targetNpv = npv - calc.nonSensNPV();
            }
            else
            {
                targetNpv *= discountCurve.discount(npvDate);
                targetNpv -= calc.nonSensNPV();
            }

            if (targetNpv.IsEqual(0.0))
            {
                return 0.0;
            }

            var bps = calc.bps();
            QLNet.Utils.QL_REQUIRE(bps.IsNotEqual(0.0), () => "null bps: impossible atm rate");

            return targetNpv.Value / bps;
        }

        // NPV of the cash flows.
        // The NPV is the sum of the cash flows, each discounted
        // according to the given constant interest rate.  The result
        // is affected by the choice of the interest-rate compounding
        // and the relative frequency and day counter.
        public static double npv(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var npv = 0.0;
            var discount = 1.0;
            var lastDate = npvDate;
            var dc = yield.dayCounter();

            for (var i = 0; i < leg.Count; ++i)
            {
                if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
                {
                    continue;
                }

                var amount = leg[i].amount();
                if (leg[i].tradingExCoupon(settlementDate))
                {
                    amount = 0.0;
                }

                var b = yield.discountFactor(getStepwiseDiscountTime(leg[i], dc, npvDate, lastDate));
                discount *= b;
                lastDate = leg[i].date();

                npv += amount * discount;
            }

            return npv;
        }

        public static double npv(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null) =>
            npv(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                includeSettlementDateFlows, settlementDate, npvDate);

        //! Basis-point sensitivity of the cash flows.
        // The result is the change in NPV due to a uniform
        // 1-basis-point change in the rate paid by the cash
        // flows. The change for each coupon is discounted according
        // to the given constant interest rate.  The result is
        // affected by the choice of the interest-rate compounding
        // and the relative frequency and day counter.

        public static double bps(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var flatRate = new FlatForward(settlementDate, yield.rate(), yield.dayCounter(),
                yield.compounding(), yield.frequency());
            return bps(leg, flatRate, includeSettlementDateFlows, settlementDate, npvDate);
        }

        public static double bps(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null) =>
            bps(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                includeSettlementDateFlows, settlementDate, npvDate);

        //! NPV of a single cash flows
        public static double npv(CashFlow cashflow, YieldTermStructure discountCurve,
            Date settlementDate = null, Date npvDate = null, int exDividendDays = 0)
        {
            var NPV = 0.0;

            if (cashflow == null)
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            if (!cashflow.hasOccurred(settlementDate + exDividendDays))
            {
                NPV = cashflow.amount() * discountCurve.discount(cashflow.date());
            }

            return NPV / discountCurve.discount(npvDate);
        }

        //! CASH of the cash flows. The CASH is the sum of the cash flows.
        public static double cash(Leg cashflows, Date settlementDate = null, int exDividendDays = 0)
        {
            if (cashflows.Count == 0)
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            var totalCASH = cashflows.Where(x => !x.hasOccurred(settlementDate + exDividendDays)).Sum(c => c.amount());

            return totalCASH;
        }

        //! Implied internal rate of return.
        // The function verifies
        // the theoretical existance of an IRR and numerically
        // establishes the IRR to the desired precision.
        public static double yield(Leg leg, double npv, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null,
            double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.05)
        {
            var solver = new NewtonSafe();
            solver.setMaxEvaluations(maxIterations);
            var objFunction = new IrrFinder(leg, npv,
                dayCounter, compounding, frequency,
                includeSettlementDateFlows,
                settlementDate, npvDate);
            return solver.solve(objFunction, accuracy, guess, guess / 10.0);
        }

        //! Cash-flow duration.
        public static double duration(Leg leg, InterestRate rate, Duration.Type type, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            switch (type)
            {
                case Duration.Type.Simple:
                    return simpleDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
                case Duration.Type.Modified:
                    return modifiedDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
                case Duration.Type.Macaulay:
                    return macaulayDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
                default:
                    QLNet.Utils.QL_FAIL("unknown duration ExerciseType");
                    break;
            }

            return 0.0;
        }

        public static double duration(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Duration.Type type, bool includeSettlementDateFlows, Date settlementDate = null,
            Date npvDate = null) =>
            duration(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                type, includeSettlementDateFlows, settlementDate, npvDate);

        //! Cash-flow convexity
        public static double convexity(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var dc = yield.dayCounter();

            var P = 0.0;
            var t = 0.0;
            var d2Pdy2 = 0.0;
            var r = yield.rate();
            var N = (int)yield.frequency();
            var lastDate = npvDate;

            for (var i = 0; i < leg.Count; ++i)
            {
                if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
                {
                    continue;
                }

                var c = leg[i].amount();
                if (leg[i].tradingExCoupon(settlementDate))
                {
                    c = 0.0;
                }

                t += getStepwiseDiscountTime(leg[i], dc, npvDate, lastDate);

                var B = yield.discountFactor(t);
                P += c * B;
                switch (yield.compounding())
                {
                    case Compounding.Simple:
                        d2Pdy2 += c * 2.0 * B * B * B * t * t;
                        break;
                    case Compounding.Compounded:
                        d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + r / N) * (1 + r / N));
                        break;
                    case Compounding.Continuous:
                        d2Pdy2 += c * B * t * t;
                        break;
                    case Compounding.SimpleThenCompounded:
                        if (t <= 1.0 / N)
                        {
                            d2Pdy2 += c * 2.0 * B * B * B * t * t;
                        }
                        else
                        {
                            d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + r / N) * (1 + r / N));
                        }

                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown compounding convention (" + yield.compounding() + ")");
                        break;
                }

                lastDate = leg[i].date();
            }

            if (P.IsEqual(0.0))
                // no cashflows
            {
                return 0.0;
            }

            return d2Pdy2 / P;
        }

        public static double convexity(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null) =>
            convexity(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                includeSettlementDateFlows, settlementDate, npvDate);

        //! Basis-point value
        /*! Obtained by setting dy = 0.0001 in the 2nd-order Taylor
            series expansion.
        */
        public static double basisPointValue(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var npv = CashFlows.npv(leg, yield, includeSettlementDateFlows, settlementDate, npvDate);
            var modifiedDuration = duration(leg, yield, Duration.Type.Modified, includeSettlementDateFlows,
                settlementDate, npvDate);
            var convexity = CashFlows.convexity(leg, yield, includeSettlementDateFlows, settlementDate, npvDate);
            var delta = -modifiedDuration * npv;
            var gamma = convexity / 100.0 * npv;

            var shift = 0.0001;
            delta *= shift;
            gamma *= shift * shift;

            return delta + 0.5 * gamma;
        }

        public static double basisPointValue(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null) =>
            basisPointValue(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                includeSettlementDateFlows, settlementDate, npvDate);

        //! Yield value of a basis point
        /*! The yield value of a one basis point change in price is
            the derivative of the yield with respect to the price
            multiplied by 0.01
        */
        public static double yieldValueBasisPoint(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var npv = CashFlows.npv(leg, yield, includeSettlementDateFlows, settlementDate, npvDate);
            var modifiedDuration = duration(leg, yield, Duration.Type.Modified, includeSettlementDateFlows,
                settlementDate, npvDate);

            var shift = 0.01;
            return 1.0 / (-npv * modifiedDuration) * shift;
        }

        public static double yieldValueBasisPoint(Leg leg, double yield, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
            Date npvDate = null) =>
            yieldValueBasisPoint(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                includeSettlementDateFlows, settlementDate, npvDate);

        #endregion

        #region Z-spread utility functions

        // NPV of the cash flows.
        //  The NPV is the sum of the cash flows, each discounted
        //  according to the z-spreaded term structure.  The result
        //  is affected by the choice of the z-spread compounding
        //  and the relative frequency and day counter.
        public static double npv(Leg leg, YieldTermStructure discountCurve, double zSpread, DayCounter dc, Compounding comp,
            Frequency freq, bool includeSettlementDateFlows,
            Date settlementDate = null, Date npvDate = null)
        {
            if (leg.empty())
            {
                return 0.0;
            }

            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var discountCurveHandle = new Handle<YieldTermStructure>(discountCurve);
            var zSpreadQuoteHandle = new Handle<Quote>(new SimpleQuote(zSpread));

            var spreadedCurve = new ZeroSpreadedTermStructure(discountCurveHandle, zSpreadQuoteHandle,
                comp, freq, dc);

            spreadedCurve.enableExtrapolation(discountCurveHandle.link.allowsExtrapolation());

            return npv(leg, spreadedCurve, includeSettlementDateFlows, settlementDate, npvDate);
        }

        //! implied Z-spread.
        public static double zSpread(Leg leg, double npv, YieldTermStructure discount, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
            Date npvDate = null, double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.0)
        {
            if (settlementDate == null)
            {
                settlementDate = Settings.evaluationDate();
            }

            if (npvDate == null)
            {
                npvDate = settlementDate;
            }

            var solver = new Brent();
            solver.setMaxEvaluations(maxIterations);
            var objFunction = new ZSpreadFinder(leg, discount, npv, dayCounter, compounding, frequency,
                includeSettlementDateFlows, settlementDate, npvDate);
            var step = 0.01;
            return solver.solve(objFunction, accuracy, guess, step);
        }

        //! deprecated implied Z-spread.
        public static double zSpread(Leg leg, YieldTermStructure d, double npv, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
            Date npvDate = null, double accuracy = 1.0e-10, int maxIterations = 100,
            double guess = 0.0) =>
            zSpread(leg, npv, d, dayCounter, compounding, frequency,
                includeSettlementDateFlows, settlementDate, npvDate,
                accuracy, maxIterations, guess);

        #endregion
    }
}
