﻿//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.processes;
using QLNet.Quotes;
using QLNet.Time;
using System;

namespace QLNet.Pricingengines.Option
{
    /// <summary>
    /// Kirk approximation for European spread option on futures
    /// </summary>
    public class KirkSpreadOptionEngine : SpreadOption.Engine
    {
        public KirkSpreadOptionEngine(BlackProcess process1,
                                      BlackProcess process2,
                                      Handle<Quote> correlation)
        {
            process1_ = process1;
            process2_ = process2;
            rho_ = correlation;
        }

        public override void calculate()
        {
            // First: tests on types
            Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European, () =>
                             "not an European Option");

            PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "not a plain-vanilla payoff");

            // forward values - futures, so b=0
            double forward1 = process1_.stateVariable().link.value();
            double forward2 = process2_.stateVariable().link.value();

            Date exerciseDate = arguments_.exercise.lastDate();

            // Volatilities
            double sigma1 = process1_.blackVolatility().link.blackVol(exerciseDate,
                                                                      forward1);
            double sigma2 = process2_.blackVolatility().link.blackVol(exerciseDate,
                                                                      forward2);

            double riskFreeDiscount = process1_.riskFreeRate().link.discount(exerciseDate);

            double strike = payoff.strike();

            // Unique F (forward) value for pricing
            double F = forward1 / (forward2 + strike);

            // Its volatility
            double sigma =
               System.Math.Sqrt(System.Math.Pow(sigma1, 2)
                         + System.Math.Pow(sigma2 * (forward2 / (forward2 + strike)), 2)
                         - 2 * rho_.link.value() * sigma1 * sigma2 * (forward2 / (forward2 + strike)));

            // Day counter and Dates handling variables
            DayCounter rfdc = process1_.riskFreeRate().link.dayCounter();
            double t = rfdc.yearFraction(process1_.riskFreeRate().link.referenceDate(),
                                         arguments_.exercise.lastDate());

            // Black-Scholes solution values
            double d1 = (System.Math.Log(F) + 0.5 * System.Math.Pow(sigma,
                                                      2) * t) / (sigma * System.Math.Sqrt(t));
            double d2 = d1 - sigma * System.Math.Sqrt(t);

            NormalDistribution pdf = new NormalDistribution();
            CumulativeNormalDistribution cum = new CumulativeNormalDistribution();
            double Nd1 = cum.value(d1);
            double Nd2 = cum.value(d2);
            double NMd1 = cum.value(-d1);
            double NMd2 = cum.value(-d2);

            QLNet.Option.Type optionType = payoff.optionType();

            if (optionType == QLNet.Option.Type.Call)
            {
                results_.value = riskFreeDiscount * (F * Nd1 - Nd2) * (forward2 + strike);
            }
            else
            {
                results_.value = riskFreeDiscount * (NMd2 - F * NMd1) * (forward2 + strike);
            }

            double? callValue = optionType == QLNet.Option.Type.Call ? results_.value :
                                 riskFreeDiscount * (F * Nd1 - Nd2) * (forward2 + strike);
            results_.theta = System.Math.Log(riskFreeDiscount) / t * callValue +
                             riskFreeDiscount * (forward1 * sigma) / (2 * System.Math.Sqrt(t)) * pdf.value(d1);
        }

        private BlackProcess process1_;
        private BlackProcess process2_;
        private Handle<Quote> rho_;
    }
}
