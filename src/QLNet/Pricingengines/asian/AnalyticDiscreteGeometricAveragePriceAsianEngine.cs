﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
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
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;

namespace QLNet.PricingEngines.asian
{
    //! Pricing engine for European discrete geometric average price Asian
    /*! This class implements a discrete geometric average price Asian
       option, with European exercise.  The formula is from "Asian
       Option", E. Levy (1997) in "Exotic Options: The State of the
       Art", edited by L. Clewlow, C. Strickland, pag 65-97

       \todo implement correct theta, rho, and dividend-rho calculation

       \test
       - the correctness of the returned value is tested by
          reproducing results available in literature.
       - the correctness of the available greeks is tested against
          numerical calculations.

       \ingroup asianengines
    */
    [PublicAPI]
    public class AnalyticDiscreteGeometricAveragePriceAsianEngine : DiscreteAveragingAsianOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public AnalyticDiscreteGeometricAveragePriceAsianEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            /* this engine cannot really check for the averageType==Geometric
               since it can be used as control variate for the Arithmetic version
               QL_REQUIRE(arguments_.averageType == Average::Geometric,"not a geometric average option")
            */
            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European Option");

            double runningLog;
            int pastFixings;
            if (arguments_.averageType == Average.Type.Geometric)
            {
                QLNet.Utils.QL_REQUIRE(arguments_.runningAccumulator > 0.0, () =>
                    "positive running product required: " + arguments_.runningAccumulator + " not allowed");

                runningLog = System.Math.Log(arguments_.runningAccumulator.GetValueOrDefault());
                pastFixings = arguments_.pastFixings.GetValueOrDefault();
            }
            else
            {
                // it is being used as control variate
                runningLog = 1.0;
                pastFixings = 0;
            }

            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var referenceDate = process_.riskFreeRate().link.referenceDate();
            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();
            var fixingTimes = new List<double>();
            int i;
            for (i = 0; i < arguments_.fixingDates.Count; i++)
            {
                if (arguments_.fixingDates[i] >= referenceDate)
                {
                    var t = voldc.yearFraction(referenceDate, arguments_.fixingDates[i]);
                    fixingTimes.Add(t);
                }
            }

            var remainingFixings = fixingTimes.Count;
            var numberOfFixings = pastFixings + remainingFixings;
            double N = numberOfFixings;

            var pastWeight = pastFixings / N;
            var futureWeight = 1.0 - pastWeight;

            double timeSum = 0;
            fixingTimes.ForEach((ii, vv) => timeSum += fixingTimes[ii]);

            var vola = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), payoff.strike());
            var temp = 0.0;
            for (i = pastFixings + 1; i < numberOfFixings; i++)
            {
                temp += fixingTimes[i - pastFixings - 1] * (N - i);
            }

            var variance = vola * vola / N / N * (timeSum + 2.0 * temp);
            var dsigG_dsig = System.Math.Sqrt(timeSum + 2.0 * temp) / N;
            var sigG = vola * dsigG_dsig;
            var dmuG_dsig = -(vola * timeSum) / N;

            var exDate = arguments_.exercise.lastDate();
            var dividendRate = process_.dividendYield().link.zeroRate(exDate, divdc, Compounding.Continuous, Frequency.NoFrequency).rate();
            var riskFreeRate = process_.riskFreeRate().link.zeroRate(exDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).rate();
            var nu = riskFreeRate - dividendRate - 0.5 * vola * vola;

            var s = process_.stateVariable().link.value();
            QLNet.Utils.QL_REQUIRE(s > 0.0, () => "positive underlying value required");

            var M = pastFixings == 0 ? 1 : pastFixings;
            var muG = pastWeight * runningLog / M + futureWeight * System.Math.Log(s) + nu * timeSum / N;
            var forwardPrice = System.Math.Exp(muG + variance / 2.0);

            var riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());

            var black = new BlackCalculator(payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

            results_.value = black.value();
            results_.delta = futureWeight * black.delta(forwardPrice) * forwardPrice / s;
            results_.gamma = forwardPrice * futureWeight / (s * s)
                             * (black.gamma(forwardPrice) * futureWeight * forwardPrice
                                - pastWeight * black.delta(forwardPrice));

            double Nx_1, nx_1;
            var CND = new CumulativeNormalDistribution();
            var ND = new NormalDistribution();
            if (sigG > Const.QL_EPSILON)
            {
                var x_1 = (muG - System.Math.Log(payoff.strike()) + variance) / sigG;
                Nx_1 = CND.value(x_1);
                nx_1 = ND.value(x_1);
            }
            else
            {
                Nx_1 = muG > System.Math.Log(payoff.strike()) ? 1.0 : 0.0;
                nx_1 = 0.0;
            }

            results_.vega = forwardPrice * riskFreeDiscount *
                            ((dmuG_dsig + sigG * dsigG_dsig) * Nx_1 + nx_1 * dsigG_dsig);

            if (payoff.optionType() == QLNet.Option.Type.Put)
            {
                results_.vega -= riskFreeDiscount * forwardPrice *
                                 (dmuG_dsig + sigG * dsigG_dsig);
            }

            var tRho = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(),
                arguments_.exercise.lastDate());
            results_.rho = black.rho(tRho) * timeSum / (N * tRho)
                           - (tRho - timeSum / N) * results_.value;

            var tDiv = divdc.yearFraction(
                process_.dividendYield().link.referenceDate(),
                arguments_.exercise.lastDate());

            results_.dividendRho = black.dividendRho(tDiv) * timeSum / (N * tDiv);

            results_.strikeSensitivity = black.strikeSensitivity();

            results_.theta = Utils.blackScholesTheta(process_,
                results_.value.GetValueOrDefault(),
                results_.delta.GetValueOrDefault(),
                results_.gamma.GetValueOrDefault());
        }
    }
}
