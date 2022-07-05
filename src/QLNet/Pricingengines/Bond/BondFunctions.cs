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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Exceptions;
using QLNet.Extensions;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Pricingengines.Bond
{
    //! Bond adapters of CashFlows functions
    /*! See CashFlows for functions' documentation.

        These adapters calls into CashFlows functions passing as input the
        Bond cashflows, the dirty price (i.e. npv) calculated from clean
        price, the bond settlementDate date (unless another date is given), zero
        ex-dividend days, and excluding any cashflow on the settlementDate date.

        Prices are always clean, as per market convention.
    */
    [PublicAPI]
    public class BondFunctions
    {
        #region Raw Functions

        public static DateTime WeightedAverageLife(DateTime today, List<double> amounts, List<DateTime> schedule)
        {
            Utils.QL_REQUIRE(amounts.Count == schedule.Count, () => "Amount list is incompatible with schedule");

            var totAmount = amounts.Where((t, x) => schedule[x] > today).Sum();

            if (totAmount.IsEqual(0))
            {
                return today;
            }

            double wal = 0;
            DayCounter dc = new Actual365Fixed();

            for (var x = 0; x < amounts.Count; x++)
            {
                if (schedule[x] <= today)
                {
                    continue;
                }

                var per = amounts[x] / totAmount;
                var years = dc.yearFraction(today, schedule[x]);
                var yearw = years * per;
                wal += yearw;
            }

            return today.AddDays(wal * 365).Date;
        }

        #endregion

        #region Date inspectors

        public static Date startDate(Instruments.Bond bond) => CashFlows.startDate(bond.cashflows());

        public static Date maturityDate(Instruments.Bond bond) => CashFlows.maturityDate(bond.cashflows());

        public static bool isTradable(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            return bond.notional(settlementDate).IsNotEqual(0.0);
        }

        #endregion

        #region CashFlow inspectors

        public static CashFlow previousCashFlow(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.previousCashFlow(bond.cashflows(), false, refDate);
        }

        public static CashFlow nextCashFlow(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.nextCashFlow(bond.cashflows(), false, refDate);
        }

        public static Date previousCashFlowDate(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.previousCashFlowDate(bond.cashflows(), false, refDate);
        }

        public static Date nextCashFlowDate(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.nextCashFlowDate(bond.cashflows(), false, refDate);
        }

        public static double? previousCashFlowAmount(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.previousCashFlowAmount(bond.cashflows(), false, refDate);
        }

        public static double? nextCashFlowAmount(Instruments.Bond bond, Date refDate = null)
        {
            if (refDate == null)
            {
                refDate = bond.settlementDate();
            }

            return CashFlows.nextCashFlowAmount(bond.cashflows(), false, refDate);
        }

        #endregion

        #region Coupon inspectors

        public static double previousCouponRate(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            return CashFlows.previousCouponRate(bond.cashflows(), false, settlementDate);
        }

        public static double nextCouponRate(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            return CashFlows.nextCouponRate(bond.cashflows(), false, settlementDate);
        }

        public static Date accrualStartDate(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accrualStartDate(bond.cashflows(), false, settlementDate);
        }

        public static Date accrualEndDate(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accrualEndDate(bond.cashflows(), false, settlementDate);
        }

        public static Date referencePeriodStart(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.referencePeriodStart(bond.cashflows(), false, settlementDate);
        }

        public static Date referencePeriodEnd(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.referencePeriodEnd(bond.cashflows(), false, settlementDate);
        }

        public static double accrualPeriod(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accrualPeriod(bond.cashflows(), false, settlementDate);
        }

        public static int accrualDays(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accrualDays(bond.cashflows(), false, settlementDate);
        }

        public static double accruedPeriod(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accruedPeriod(bond.cashflows(), false, settlementDate);
        }

        public static double accruedDays(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accruedDays(bond.cashflows(), false, settlementDate);
        }

        public static double accruedAmount(Instruments.Bond bond, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.accruedAmount(bond.cashflows(), false, settlementDate) * 100.0 / bond.notional(settlementDate);
        }

        #endregion

        #region YieldTermStructure functions

        public static double cleanPrice(Instruments.Bond bond, YieldTermStructure discountCurve, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " settlementDate date (maturity being " +
                    bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = CashFlows.npv(bond.cashflows(), discountCurve, false, settlementDate) *
                100.0 / bond.notional(settlementDate);
            return dirtyPrice - bond.accruedAmount(settlementDate);
        }

        public static double bps(Instruments.Bond bond, YieldTermStructure discountCurve, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.bps(bond.cashflows(), discountCurve, false, settlementDate) * 100.0 / bond.notional(settlementDate);
        }

        public static double atmRate(Instruments.Bond bond, YieldTermStructure discountCurve, Date settlementDate = null, double? cleanPrice = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = cleanPrice == null ? null : cleanPrice + bond.accruedAmount(settlementDate);
            var currentNotional = bond.notional(settlementDate);
            var npv = dirtyPrice / 100.0 * currentNotional;

            return CashFlows.atmRate(bond.cashflows(), discountCurve, false, settlementDate, settlementDate, npv);
        }

        #endregion

        #region Yield (a.k.a. Internal Rate of Return, i.e. IRR) functions

        public static double cleanPrice(Instruments.Bond bond, InterestRate yield, Date settlementDate = null) => dirtyPrice(bond, yield, settlementDate) - bond.accruedAmount(settlementDate);

        public static double cleanPrice(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null) =>
            cleanPrice(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);

        public static double dirtyPrice(Instruments.Bond bond, InterestRate yield, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = CashFlows.npv(bond.cashflows(), yield, false, settlementDate) *
                100.0 / bond.notional(settlementDate);
            return dirtyPrice;
        }

        public static double dirtyPrice(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null) =>
            dirtyPrice(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);

        public static double bps(Instruments.Bond bond, InterestRate yield, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.bps(bond.cashflows(), yield, false, settlementDate) *
                100.0 / bond.notional(settlementDate);
        }

        public static double bps(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null) =>
            bps(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);

        public static double yield(Instruments.Bond bond, double cleanPrice, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null, double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.05)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = cleanPrice + bond.accruedAmount(settlementDate);
            dirtyPrice /= 100.0 / bond.notional(settlementDate);

            return CashFlows.yield(bond.cashflows(), dirtyPrice,
                dayCounter, compounding, frequency,
                false, settlementDate, settlementDate,
                accuracy, maxIterations, guess);
        }

        public static double duration(Instruments.Bond bond, InterestRate yield, Duration.Type type = Duration.Type.Modified,
            Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.duration(bond.cashflows(), yield, type, false, settlementDate);
        }

        public static double duration(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Duration.Type type = Duration.Type.Modified, Date settlementDate = null) =>
            duration(bond, new InterestRate(yield, dayCounter, compounding, frequency), type, settlementDate);

        public static double convexity(Instruments.Bond bond, InterestRate yield, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.convexity(bond.cashflows(), yield, false, settlementDate);
        }

        public static double convexity(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null) =>
            convexity(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);

        public static double basisPointValue(Instruments.Bond bond, InterestRate yield, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.basisPointValue(bond.cashflows(), yield,
                false, settlementDate);
        }

        public static double basisPointValue(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
            Date settlementDate = null) =>
            CashFlows.basisPointValue(bond.cashflows(), new InterestRate(yield, dayCounter, compounding, frequency), false, settlementDate);

        public static double yieldValueBasisPoint(Instruments.Bond bond, InterestRate yield, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            return CashFlows.yieldValueBasisPoint(bond.cashflows(), yield,
                false, settlementDate);
        }

        public static double yieldValueBasisPoint(Instruments.Bond bond, double yield, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, Date settlementDate = null) =>
            CashFlows.yieldValueBasisPoint(bond.cashflows(), new InterestRate(yield, dayCounter, compounding, frequency), false, settlementDate);

        #endregion

        #region Z-spread functions

        public static double cleanPrice(Instruments.Bond bond, YieldTermStructure discount, double zSpread, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, Date settlementDate = null)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = CashFlows.npv(bond.cashflows(), discount, zSpread, dayCounter, compounding, frequency, false, settlementDate) *
                100.0 / bond.notional(settlementDate);
            return dirtyPrice - bond.accruedAmount(settlementDate);
        }

        public static double zSpread(Instruments.Bond bond, double cleanPrice, YieldTermStructure discount, DayCounter dayCounter, Compounding compounding,
            Frequency frequency, Date settlementDate = null, double accuracy = 1.0e-10, int maxIterations = 100,
            double guess = 0.0)
        {
            if (settlementDate == null)
            {
                settlementDate = bond.settlementDate();
            }

            Utils.QL_REQUIRE(isTradable(bond, settlementDate), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")",
                QLNetExceptionEnum.NotTradableException);

            var dirtyPrice = cleanPrice + bond.accruedAmount(settlementDate);
            dirtyPrice /= 100.0 / bond.notional(settlementDate);

            return CashFlows.zSpread(bond.cashflows(),
                discount,
                dirtyPrice,
                dayCounter, compounding, frequency,
                false, settlementDate, settlementDate,
                accuracy, maxIterations, guess);
        }

        #endregion
    }
}
