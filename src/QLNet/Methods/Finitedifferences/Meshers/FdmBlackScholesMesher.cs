/*
 Copyright (C) 2017, 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.processes;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;
using QLNet.Time;

/*! \file fdmblackscholesmesher.cpp
    \brief 1-d mesher for the Black-Scholes process (in ln(S))
*/

namespace QLNet.Methods.Finitedifferences.Meshers
{
    [PublicAPI]
    public class FdmBlackScholesMesher : Fdm1dMesher
    {
        public FdmBlackScholesMesher(int size,
            GeneralizedBlackScholesProcess process,
            double maturity, double strike,
            double? xMinConstraint = null,
            double? xMaxConstraint = null,
            double eps = 0.0001,
            double scaleFactor = 1.5,
            Pair<double?, double?> cPoint
                = null,
            DividendSchedule dividendSchedule = null,
            FdmQuantoHelper fdmQuantoHelper = null,
            double spotAdjustment = 0.0)
            : base(size)
        {
            var S = process.x0();
            Utils.QL_REQUIRE(S > 0.0, () => "negative or null underlying given");

            dividendSchedule = dividendSchedule == null ? new DividendSchedule() : dividendSchedule;
            var intermediateSteps = new List<pair_double>();
            for (var i = 0;
                 i < dividendSchedule.Count
                 && process.time(dividendSchedule[i].date()) <= maturity;
                 ++i)
            {
                intermediateSteps.Add(
                    new pair_double(
                        process.time(dividendSchedule[i].date()),
                        dividendSchedule[i].amount()
                    ));
            }

            var intermediateTimeSteps = (int)System.Math.Max(2, 24.0 * maturity);
            for (var i = 0; i < intermediateTimeSteps; ++i)
            {
                intermediateSteps.Add(
                    new pair_double((i + 1) * (maturity / intermediateTimeSteps), 0.0));
            }

            intermediateSteps.Sort();

            var rTS = process.riskFreeRate();
            var qTS = fdmQuantoHelper != null
                ? new Handle<YieldTermStructure>(
                    new QuantoTermStructure(process.dividendYield(),
                        process.riskFreeRate(),
                        new Handle<YieldTermStructure>(fdmQuantoHelper.foreignTermStructure()),
                        process.blackVolatility(),
                        strike,
                        new Handle<BlackVolTermStructure>(fdmQuantoHelper.fxVolatilityTermStructure()),
                        fdmQuantoHelper.exchRateATMlevel(),
                        fdmQuantoHelper.equityFxCorrelation()))
                : process.dividendYield();

            var lastDivTime = 0.0;
            var fwd = S + spotAdjustment;
            double mi = fwd, ma = fwd;

            for (var i = 0; i < intermediateSteps.Count; ++i)
            {
                var divTime = intermediateSteps[i].first;
                var divAmount = intermediateSteps[i].second;

                fwd = fwd / rTS.currentLink().discount(divTime) * rTS.currentLink().discount(lastDivTime)
                                                                * qTS.currentLink().discount(divTime) / qTS.currentLink().discount(lastDivTime);

                mi = System.Math.Min(mi, fwd);
                ma = System.Math.Max(ma, fwd);

                fwd -= divAmount;

                mi = System.Math.Min(mi, fwd);
                ma = System.Math.Max(ma, fwd);

                lastDivTime = divTime;
            }

            // Set the grid boundaries
            var normInvEps = new InverseCumulativeNormal().value(1 - eps);
            var sigmaSqrtT
                = process.blackVolatility().currentLink().blackVol(maturity, strike)
                  * System.Math.Sqrt(maturity);

            double? xMin = System.Math.Log(mi) - sigmaSqrtT * normInvEps * scaleFactor;
            double? xMax = System.Math.Log(ma) + sigmaSqrtT * normInvEps * scaleFactor;

            if (xMinConstraint != null)
            {
                xMin = xMinConstraint;
            }

            if (xMaxConstraint != null)
            {
                xMax = xMaxConstraint;
            }

            Fdm1dMesher helper;
            if (cPoint != null
                && cPoint.first != null
                && System.Math.Log(cPoint.first.Value) >= xMin && System.Math.Log(cPoint.first.Value) <= xMax)
            {
                helper = new Concentrating1dMesher(xMin.Value, xMax.Value, size,
                    new Pair<double?, double?>(System.Math.Log(cPoint.first.Value), cPoint.second));
            }
            else
            {
                helper = new Uniform1dMesher(xMin.Value, xMax.Value, size);
            }

            locations_ = helper.locations();
            for (var i = 0; i < locations_.Count; ++i)
            {
                dplus_[i] = helper.dplus(i);
                dminus_[i] = helper.dminus(i);
            }
        }

        public static GeneralizedBlackScholesProcess processHelper(Handle<Quote> s0,
            Handle<YieldTermStructure> rTS,
            Handle<YieldTermStructure> qTS,
            double vol) =>
            new GeneralizedBlackScholesProcess(
                s0, qTS, rTS,
                new Handle<BlackVolTermStructure>(
                    new BlackConstantVol(rTS.currentLink().referenceDate(),
                        new Calendar(),
                        vol,
                        rTS.currentLink().dayCounter())));
    }
}
