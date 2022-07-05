//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;

namespace QLNet.PricingEngines.asian
{
    //! Pricing engine for European discrete geometric average-strike Asian option
    /*! This class implements a discrete geometric average-strike Asian
        option, with European exercise.  The formula is from "Asian
        Option", E. Levy (1997) in "Exotic Options: The State of the
        Art", edited by L. Clewlow, C. Strickland, pag 65-97

        \test
        - the correctness of the returned value is tested by
          reproducing known good results.

        \ingroup asianengines
    */
    [PublicAPI]
    public class AnalyticDiscreteGeometricAverageStrikeAsianEngine : DiscreteAveragingAsianOption.Engine
    {
        private GeneralizedBlackScholesProcess process_;

        public AnalyticDiscreteGeometricAverageStrikeAsianEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Geometric, () =>
                "not a geometric average option");

            QLNet.Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () =>
                "not an European option");

            QLNet.Utils.QL_REQUIRE(arguments_.runningAccumulator > 0.0, () =>
                "positive running product required: " + arguments_.runningAccumulator + "not allowed");

            var runningLog = System.Math.Log(arguments_.runningAccumulator.GetValueOrDefault());
            var pastFixings = arguments_.pastFixings;
            QLNet.Utils.QL_REQUIRE(pastFixings == 0, () => "past fixings currently not managed");

            var payoff = arguments_.payoff as PlainVanillaPayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();

            var fixingTimes = new List<double>();
            for (var i = 0; i < arguments_.fixingDates.Count; i++)
            {
                if (arguments_.fixingDates[i] >= arguments_.fixingDates[0])
                {
                    var t = voldc.yearFraction(arguments_.fixingDates[0], arguments_.fixingDates[i]);
                    fixingTimes.Add(t);
                }
            }

            var remainingFixings = fixingTimes.Count;
            var numberOfFixings = pastFixings.GetValueOrDefault() + remainingFixings;
            double N = numberOfFixings;

            var pastWeight = pastFixings.GetValueOrDefault() / N;
            var futureWeight = 1.0 - pastWeight;

            double timeSum = 0;
            fixingTimes.ForEach((ii, vv) => timeSum += fixingTimes[ii]);

            var residualTime = rfdc.yearFraction(arguments_.fixingDates[pastFixings.GetValueOrDefault()],
                arguments_.exercise.lastDate());

            var underlying = process_.stateVariable().link.value();
            QLNet.Utils.QL_REQUIRE(underlying > 0.0, () => "positive underlying value required");

            var volatility = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), underlying);

            var exDate = arguments_.exercise.lastDate();
            var dividendRate = process_.dividendYield().link.zeroRate(exDate, divdc,
                Compounding.Continuous, Frequency.NoFrequency).value();

            var riskFreeRate = process_.riskFreeRate().link.zeroRate(exDate, rfdc,
                Compounding.Continuous, Frequency.NoFrequency).value();

            var nu = riskFreeRate - dividendRate - 0.5 * volatility * volatility;

            var temp = 0.0;
            for (var i = pastFixings.GetValueOrDefault() + 1; i < numberOfFixings; i++)
            {
                temp += fixingTimes[i - pastFixings.GetValueOrDefault() - 1] * (N - i);
            }

            var variance = volatility * volatility / N / N * (timeSum + 2.0 * temp);
            var covarianceTerm = volatility * volatility / N * timeSum;
            var sigmaSum_2 = variance + volatility * volatility * residualTime - 2.0 * covarianceTerm;

            var M = pastFixings.GetValueOrDefault() == 0 ? 1 : pastFixings.GetValueOrDefault();
            var runningLogAverage = runningLog / M;

            var muG = pastWeight * runningLogAverage +
                      futureWeight * System.Math.Log(underlying) +
                      nu * timeSum / N;

            var f = new CumulativeNormalDistribution();

            var y1 = (System.Math.Log(underlying) +
                         (riskFreeRate - dividendRate) * residualTime -
                         muG - variance / 2.0 + sigmaSum_2 / 2.0)
                     / System.Math.Sqrt(sigmaSum_2);
            var y2 = y1 - System.Math.Sqrt(sigmaSum_2);

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    results_.value = underlying * System.Math.Exp(-dividendRate * residualTime)
                                                * f.value(y1) - System.Math.Exp(muG + variance / 2.0 - riskFreeRate * residualTime) * f.value(y2);
                    break;
                case QLNet.Option.Type.Put:
                    results_.value = -underlying * System.Math.Exp(-dividendRate * residualTime)
                                                 * f.value(-y1) + System.Math.Exp(muG + variance / 2.0 - riskFreeRate * residualTime) * f.value(-y2);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                    break;
            }
        }
    }
}
