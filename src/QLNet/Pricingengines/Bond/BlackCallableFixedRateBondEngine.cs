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

using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Instruments;
using QLNet.Instruments.Bonds;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Bond;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.PricingEngines.Bond
{
    //! Black-formula callable fixed rate bond engine
    /*! Callable fixed rate bond Black engine. The embedded (European)
        option follows the Black "European bond option" treatment in
        Hull, Fourth Edition, Chapter 20.

        \todo set additionalResults (e.g. vega, fairStrike, etc.)

        \warning This class has yet to be tested

        \ingroup callablebondengines
    */
    [PublicAPI]
    public class BlackCallableFixedRateBondEngine : CallableBond.Engine
    {
        private Handle<YieldTermStructure> discountCurve_;
        private Handle<CallableBondVolatilityStructure> volatility_;

        //! volatility is the quoted fwd yield volatility, not price vol
        public BlackCallableFixedRateBondEngine(Handle<Quote> fwdYieldVol, Handle<YieldTermStructure> discountCurve)
        {
            volatility_ = new Handle<CallableBondVolatilityStructure>(new CallableBondConstantVolatility(0, new NullCalendar(),
                fwdYieldVol,
                new Actual365Fixed()));
            discountCurve_ = discountCurve;

            volatility_.registerWith(update);
            discountCurve_.registerWith(update);
        }

        //! volatility is the quoted fwd yield volatility, not price vol
        public BlackCallableFixedRateBondEngine(Handle<CallableBondVolatilityStructure> yieldVolStructure,
            Handle<YieldTermStructure> discountCurve)
        {
            volatility_ = yieldVolStructure;
            discountCurve_ = discountCurve;
            volatility_.registerWith(update);
            discountCurve_.registerWith(update);
        }

        public override void calculate()
        {
            // validate args for Black engine
            QLNet.Utils.QL_REQUIRE(arguments_.putCallSchedule.Count == 1, () => "Must have exactly one call/put date to use Black Engine");

            var settle = arguments_.settlementDate;
            var exerciseDate = arguments_.callabilityDates[0];
            QLNet.Utils.QL_REQUIRE(exerciseDate >= settle, () => "must have exercise Date >= settlement Date");

            var fixedLeg = arguments_.cashflows;

            var value = CashFlows.npv(fixedLeg, discountCurve_, false, settle);

            var npv = CashFlows.npv(fixedLeg, discountCurve_, false, discountCurve_.link.referenceDate());

            var fwdCashPrice = (value - spotIncome()) /
                               discountCurve_.link.discount(exerciseDate);

            var cashStrike = arguments_.callabilityPrices[0];

            var type = arguments_.putCallSchedule[0].type() ==
                       Callability.Type.Call
                ? QLNet.Option.Type.Call
                : QLNet.Option.Type.Put;

            var priceVol = forwardPriceVolatility();

            var exerciseTime = volatility_.link.dayCounter().yearFraction(
                volatility_.link.referenceDate(),
                exerciseDate);
            var embeddedOptionValue = Utils.blackFormula(type,
                cashStrike,
                fwdCashPrice,
                priceVol * System.Math.Sqrt(exerciseTime));

            if (type == QLNet.Option.Type.Call)
            {
                results_.value = npv - embeddedOptionValue;
                results_.settlementValue = value - embeddedOptionValue;
            }
            else
            {
                results_.value = npv + embeddedOptionValue;
                results_.settlementValue = value + embeddedOptionValue;
            }
        }

        // converts the yield volatility into a forward price volatility
        private double forwardPriceVolatility()
        {
            var bondMaturity = arguments_.redemptionDate;
            var exerciseDate = arguments_.callabilityDates[0];
            var fixedLeg = arguments_.cashflows;

            // value of bond cash flows at option maturity
            var fwdNpv = CashFlows.npv(fixedLeg, discountCurve_, false, exerciseDate);

            var dayCounter = arguments_.paymentDayCounter;
            var frequency = arguments_.frequency;

            // adjust if zero coupon bond (see also bond.cpp)
            if (frequency == Frequency.NoFrequency || frequency == Frequency.Once)
            {
                frequency = Frequency.Annual;
            }

            var fwdYtm = CashFlows.yield(fixedLeg,
                fwdNpv,
                dayCounter,
                Compounding.Compounded,
                frequency,
                false,
                exerciseDate);

            var fwdRate = new InterestRate(fwdYtm, dayCounter, Compounding.Compounded, frequency);

            var fwdDur = CashFlows.duration(fixedLeg,
                fwdRate,
                Duration.Type.Modified, false,
                exerciseDate);

            var cashStrike = arguments_.callabilityPrices[0];
            dayCounter = volatility_.link.dayCounter();
            var referenceDate = volatility_.link.referenceDate();
            var exerciseTime = dayCounter.yearFraction(referenceDate,
                exerciseDate);
            var maturityTime = dayCounter.yearFraction(referenceDate,
                bondMaturity);
            var yieldVol = volatility_.link.volatility(exerciseTime,
                maturityTime - exerciseTime,
                cashStrike);
            var fwdPriceVol = yieldVol * fwdDur * fwdYtm;
            return fwdPriceVol;
        }

        // present value of all coupons paid during the life of option
        private double spotIncome()
        {
            //! settle date of embedded option assumed same as that of bond
            var settlement = arguments_.settlementDate;
            var cf = arguments_.cashflows;
            var optionMaturity = arguments_.putCallSchedule[0].date();

            /* the following assumes
               1. cashflows are in ascending order !
               2. income = coupons paid between settlementDate() and put/call date
            */
            var income = 0.0;
            for (var i = 0; i < cf.Count - 1; ++i)
            {
                if (!cf[i].hasOccurred(settlement, false))
                {
                    if (cf[i].hasOccurred(optionMaturity, false))
                    {
                        income += cf[i].amount() *
                                  discountCurve_.link.discount(cf[i].date());
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return income / discountCurve_.link.discount(settlement);
        }
    }

    //! Black-formula callable zero coupon bond engine
    /*! Callable zero coupon bond, where the embedded (European)
        option price is assumed to obey the Black formula. Follows
        "European bond option" treatment in Hull, Fourth Edition,
        Chapter 20.

        \warning This class has yet to be tested.

        \ingroup callablebondengines
    */
}
