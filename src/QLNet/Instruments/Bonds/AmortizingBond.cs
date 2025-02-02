﻿/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;
using QLNet.Time;
using QLNet.Time.Calendars;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class AmortizingBond : Bond
    {
        protected Calendar _calendar;
        protected double _couponRate;
        protected DayCounter _dCounter;
        protected double _faceValue;
        protected bool _isPremium;
        protected Date _issueDate;
        protected double _marketValue;
        protected Date _maturityDate;
        protected AmortizingMethod _method;
        protected double _originalPayment;
        protected Frequency _payFrequency;
        protected Date _tradeDate;
        protected double _yield;

        public AmortizingBond(double FaceValue,
            double MarketValue,
            double CouponRate,
            Date IssueDate,
            Date MaturityDate,
            Date TradeDate,
            Frequency payFrequency,
            DayCounter dCounter,
            AmortizingMethod Method,
            Calendar calendar,
            double gYield = 0) :
            base(0, new TARGET(), IssueDate)
        {
            _faceValue = FaceValue;
            _marketValue = MarketValue;
            _couponRate = CouponRate;
            _issueDate = IssueDate;
            _maturityDate = MaturityDate;
            _tradeDate = TradeDate;
            _payFrequency = payFrequency;
            _dCounter = dCounter;
            _method = Method;
            _calendar = calendar;
            _isPremium = _marketValue > _faceValue;

            // Store regular payment of faceValue * couponRate for later calculation
            _originalPayment = _faceValue * _couponRate / (double)_payFrequency;

            if (gYield.IsEqual(0.0))
            {
                _yield = calculateYield();
            }
            else
            {
                _yield = gYield;
            }

            // We can have several method here
            //  Straight-Line Amortization , Effective Interest Rate, Rule 78
            // for now we start with Effective Interest Rate.
            switch (_method)
            {
                case AmortizingMethod.EffectiveInterestRate:
                    addEffectiveInterestRateAmortizing();
                    break;
            }
        }

        public double AmortizationValue(Date d)
        {
            // Check Date
            if (d < _tradeDate || d > _maturityDate)
            {
                return 0;
            }

            double totAmortized = 0;
            var lastDate = _tradeDate;
            foreach (var c in cashflows_)
            {
                if (c.date() <= d)
                {
                    lastDate = c.date();
                    if (c is AmortizingPayment)
                    {
                        totAmortized += (c as AmortizingPayment).amount();
                    }
                }
                else
                {
                    break;
                }
            }

            if (lastDate < d)
            {
                // lastDate < d let calculate last interest

                // Base Interest
                var r1 = new InterestRate(_couponRate, _dCounter, Compounding.Simple, _payFrequency);
                var c1 = new FixedRateCoupon(d, _faceValue, r1, lastDate, d);
                var baseInterest = c1.amount();

                //
                var r2 = new InterestRate(_yield, _dCounter, Compounding.Simple, _payFrequency);
                var c2 = new FixedRateCoupon(d, _marketValue, r2, lastDate, d);
                var yieldInterest = c2.amount();

                totAmortized += System.Math.Abs(baseInterest - yieldInterest);
            }

            if (_isPremium)
            {
                return _marketValue - totAmortized;
            }

            return _marketValue + totAmortized;
        }

        public bool isPremium() => _isPremium;

        public double Yield() => _yield;

        private void addEffectiveInterestRateAmortizing()
        {
            // Amortizing Schedule
            var schedule = new Schedule(_tradeDate, _maturityDate, new Period(_payFrequency),
                _calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward, false);
            var currentNominal = _marketValue;
            var prevDate = _tradeDate;
            var actualDate = _tradeDate;

            for (var i = 1; i < schedule.Count; ++i)
            {
                actualDate = schedule[i];
                var rate = new InterestRate(_yield, _dCounter, Compounding.Simple, Frequency.Annual);
                var rate2 = new InterestRate(_couponRate, _dCounter, Compounding.Simple, Frequency.Annual);
                FixedRateCoupon r, r2;
                if (i > 1)
                {
                    r = new FixedRateCoupon(actualDate, currentNominal, rate, prevDate, actualDate, prevDate, actualDate);
                    r2 = new FixedRateCoupon(actualDate, currentNominal, rate2, prevDate, actualDate, prevDate, actualDate, null, _originalPayment);
                }

                else
                {
                    Calendar nullCalendar = new NullCalendar();
                    var p1 = new Period(_payFrequency);
                    var testDate = nullCalendar.advance(actualDate, -1 * p1);

                    r = new FixedRateCoupon(actualDate, currentNominal, rate, testDate, actualDate, prevDate, actualDate);
                    r2 = new FixedRateCoupon(actualDate, currentNominal, rate2, testDate, actualDate, prevDate, actualDate, null, _originalPayment);
                }

                var amort = System.Math.Round(System.Math.Abs(_originalPayment - r.amount()), 2);

                var p = new AmortizingPayment(amort, actualDate);
                if (_isPremium)
                {
                    currentNominal -= System.Math.Abs(amort);
                }
                else
                {
                    currentNominal += System.Math.Abs(amort);
                }

                cashflows_.Add(r2);
                cashflows_.Add(p);
                prevDate = actualDate;
            }

            // Add single redemption for yield calculation
            setSingleRedemption(_faceValue, 100, _maturityDate);
        }

        private double calculateYield()
        {
            // We create a bond cashflow from issue to maturity just
            // to calculate effective rate ( the rate that discount _marketValue )
            var schedule = new Schedule(_issueDate, _maturityDate, new Period(_payFrequency),
                _calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward, false);

            List<CashFlow> cashflows = new FixedRateLeg(schedule)
                .withCouponRates(_couponRate, _dCounter)
                .withPaymentCalendar(_calendar)
                .withNotionals(_faceValue)
                .withPaymentAdjustment(BusinessDayConvention.Unadjusted);

            // Add single redemption for yield calculation
            var r = new Redemption(_faceValue, _maturityDate);
            cashflows.Add(r);

            // Calculate Amortizing Yield ( Effective Rate )
            var testDate = CashFlows.previousCashFlowDate(cashflows, false, _tradeDate);
            return CashFlows.yield(cashflows, _marketValue, _dCounter, Compounding.Simple, _payFrequency,
                false, testDate);
        }

        // temporary testing function
        private double calculateYield2()
        {
            var CapitalGain = _faceValue - _marketValue;
            double YearToMaturity = _maturityDate.year() - _tradeDate.year();
            var AnnualizedCapitalGain = CapitalGain / YearToMaturity;
            var AnnualInterest = _couponRate * _faceValue;
            var TotalAnnualizedReturn = AnnualizedCapitalGain + AnnualInterest;
            var yieldA = TotalAnnualizedReturn / _marketValue;
            var yieldB = TotalAnnualizedReturn / (_faceValue - AnnualizedCapitalGain);
            return (yieldA + yieldB) / 2;
        }
    }
}
