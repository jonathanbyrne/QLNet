﻿/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Math.Solvers1d;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class MBSFixedRateBond : AmortizingFixedRateBond
    {
        protected List<double> bondFactors_;
        protected DayCounter dCounter_;
        protected Period originalLength_, remainingLength_;
        protected double PassThroughRate_;
        protected IPrepayModel prepayModel_;
        protected double WACRate_;

        public MBSFixedRateBond(int settlementDays,
            Calendar calendar,
            double faceAmount,
            Date startDate,
            Period bondTenor,
            Period originalLength,
            Frequency sinkingFrequency,
            double WACRate,
            double PassThroughRate,
            DayCounter accrualDayCounter,
            IPrepayModel prepayModel,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            Date issueDate = null)
            : base(settlementDays, calendar, faceAmount, startDate, bondTenor, sinkingFrequency, WACRate, accrualDayCounter, paymentConvention, issueDate)
        {
            prepayModel_ = prepayModel;
            originalLength_ = originalLength;
            remainingLength_ = bondTenor;
            WACRate_ = WACRate;
            PassThroughRate_ = PassThroughRate;
            dCounter_ = accrualDayCounter;
            cashflows_ = expectedCashflows();
        }

        public double BondEquivalentYield() => 2 * (System.Math.Pow(1 + MonthlyYield(), 6) - 1);

        public List<double> BondFactors()
        {
            if (bondFactors_ == null)
            {
                calcBondFactor();
            }

            return bondFactors_;
        }

        public List<CashFlow> expectedCashflows()
        {
            calcBondFactor();

            var expectedcashflows = new List<CashFlow>();

            List<double> notionals = new InitializedList<double>(schedule_.Count);
            notionals[0] = notionals_[0];
            for (var i = 0; i < schedule_.Count - 1; ++i)
            {
                var currentNotional = notionals[i];
                var smm = SMM(schedule_[i]);
                var prepay = (notionals[i] * bondFactors_[i + 1]) / bondFactors_[i] * smm;
                var actualamort = currentNotional * (1 - bondFactors_[i + 1] / bondFactors_[i]);
                notionals[i + 1] = currentNotional - actualamort - prepay;

                // ADD
                CashFlow c1 = new VoluntaryPrepay(prepay, schedule_[i + 1]);
                CashFlow c2 = new AmortizingPayment(actualamort, schedule_[i + 1]);
                CashFlow c3 = new FixedRateCoupon(schedule_[i + 1], currentNotional, new InterestRate(PassThroughRate_, dCounter_, Compounding.Simple, Frequency.Annual), schedule_[i], schedule_[i + 1]);
                expectedcashflows.Add(c1);
                expectedcashflows.Add(c2);
                expectedcashflows.Add(c3);
            }

            notionals[notionals.Count - 1] = 0.0;

            return expectedcashflows;
        }

        public double MonthlyYield()
        {
            var solver = new Brent();
            solver.setMaxEvaluations(100);
            var cf = expectedCashflows();

            var objective = new MonthlyYieldFinder(notional(settlementDate()), cf, settlementDate());
            return solver.solve(objective, 1.0e-10, 0.02, 0.0, 1.0) / 100;
        }

        public double SMM(Date d)
        {
            if (prepayModel_ != null)
            {
                return prepayModel_.getSMM(d + (originalLength_ - remainingLength_));
            }

            return 0;
        }

        protected void calcBondFactor()
        {
            bondFactors_ = new InitializedList<double>(notionals_.Count);
            for (var i = 0; i < notionals_.Count; i++)
            {
                if (i == 0)
                {
                    bondFactors_[i] = 1;
                }
                else
                {
                    bondFactors_[i] = notionals_[i] / notionals_[0];
                }
            }
        }
    }
}
