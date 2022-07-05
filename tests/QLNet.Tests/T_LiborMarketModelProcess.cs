/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
using Xunit;
using QLNet.Methods.montecarlo;
using QLNet.Indexes;
using QLNet.Indexes.Ibor;
using QLNet.Time;
using QLNet.Math.statistics;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Math.Interpolations;
using QLNet.legacy.libormarketmodels;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Math.Distributions;
using QLNet.Math.RandomNumbers;
using QLNet.Termstructures.Yield;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_LiborMarketModelProcess : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_LiborMarketModelProcess()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        int len = 10;

        IborIndex makeIndex()
        {
            DayCounter dayCounter = new Actual360();
            var dates = new List<Date>();
            var rates = new List<double>();
            dates.Add(new Date(4, 9, 2005));
            dates.Add(new Date(4, 9, 2018));
            rates.Add(0.01);
            rates.Add(0.08);
            var Interpolator = new Linear();
            var termStructure = new RelinkableHandle<YieldTermStructure>();
            //termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            IborIndex index = new Euribor1Y(termStructure);

            var todaysDate =
               index.fixingCalendar().adjust(new Date(4, 9, 2005));
            Settings.setEvaluationDate(todaysDate);

            dates[0] = index.fixingCalendar().advance(todaysDate,
                                                      index.fixingDays(), TimeUnit.Days);

            //termStructure.linkTo(new ZeroCurve(dates, rates, dayCounter));
            termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            return index;
        }

        CapletVarianceCurve makeCapVolCurve(Date todaysDate)
        {
            double[] vols = {14.40, 17.15, 16.81, 16.64, 16.17,
                          15.78, 15.40, 15.21, 14.86, 14.54
                         };

            var dates = new List<Date>();
            var capletVols = new List<double>();
            var process = new LiborForwardModelProcess(len + 1, makeIndex(), null);

            for (var i = 0; i < len; ++i)
            {
                capletVols.Add(vols[i] / 100);
                dates.Add(process.fixingDates()[i + 1]);
            }

            return new CapletVarianceCurve(todaysDate, dates,
                                           capletVols, new ActualActual());
        }

        LiborForwardModelProcess makeProcess()
        {
            var volaComp = new Matrix();
            return makeProcess(volaComp);
        }

        LiborForwardModelProcess makeProcess(Matrix volaComp)
        {
            var factors = volaComp.empty() ? 1 : volaComp.columns();

            var index = makeIndex();
            var process = new LiborForwardModelProcess(len, index, null);

            LfmCovarianceParameterization fct = new LfmHullWhiteParameterization(
               process,
               makeCapVolCurve(Settings.evaluationDate()),
               volaComp * Matrix.transpose(volaComp), factors);

            process.setCovarParam(fct);

            return process;
        }

        [Fact(Skip = "LongRun")]
        public void testInitialisation()
        {
            // Testing caplet LMM process initialisation
            DayCounter dayCounter = new Actual360();
            var termStructure = new RelinkableHandle<YieldTermStructure>();
            termStructure.linkTo(Utilities.flatRate(Date.Today, 0.04, dayCounter));

            IborIndex index = new Euribor6M(termStructure);
            OptionletVolatilityStructure capletVol = new ConstantOptionletVolatility(
               termStructure.currentLink().referenceDate(),
               termStructure.currentLink().calendar(),
               BusinessDayConvention.Following,
               0.2,
               termStructure.currentLink().dayCounter());

            var calendar = index.fixingCalendar();

            for (var daysOffset = 0; daysOffset < 1825 /* 5 year*/; daysOffset += 8)
            {
                var todaysDate = calendar.adjust(Date.Today + daysOffset);
                Settings.setEvaluationDate(todaysDate);
                var settlementDate =
                   calendar.advance(todaysDate, index.fixingDays(), TimeUnit.Days);

                termStructure.linkTo(Utilities.flatRate(settlementDate, 0.04, dayCounter));

                var process = new LiborForwardModelProcess(60, index);

                var fixings = process.fixingTimes();
                for (var i = 1; i < fixings.Count - 1; ++i)
                {
                    var ileft = process.nextIndexReset(fixings[i] - 0.000001);
                    var iright = process.nextIndexReset(fixings[i] + 0.000001);
                    var ii = process.nextIndexReset(fixings[i]);

                    if (ileft != i || iright != i + 1 || ii != i + 1)
                    {
                        QAssert.Fail("Failed to next index resets");
                    }
                }

            }
        }

        [Fact(Skip = "LongRun")]
        public void testLambdaBootstrapping()
        {
            // Testing caplet LMM lambda bootstrapping
            var tolerance = 1e-10;
            double[] lambdaExpected = {14.3010297550, 19.3821411939, 15.9816590141,
                                    15.9953118303, 14.0570815635, 13.5687599894,
                                    12.7477197786, 13.7056638165, 11.6191989567
                                   };

            var process = makeProcess();
            var covar = process.covariance(0.0, null, 1.0);

            for (var i = 0; i < 9; ++i)
            {
                var calculated = System.Math.Sqrt(covar[i + 1, i + 1]);
                var expected = lambdaExpected[i] / 100;

                if (System.Math.Abs(calculated - expected) > tolerance)
                {
                    QAssert.Fail("Failed to reproduce expected lambda values"
                                 + "\n    calculated: " + calculated
                                 + "\n    expected:   " + expected);
                }
            }

            var param = process.covarParam();

            var tmp = process.fixingTimes();
            var grid = new TimeGrid(tmp.Last(), 14);

            for (var t = 0; t < grid.size(); ++t)
            {
                //verifier la presence du null
                var diff = param.integratedCovariance(grid[t])
                           - param.integratedCovariance(grid[t]);

                for (var i = 0; i < diff.rows(); ++i)
                {
                    for (var j = 0; j < diff.columns(); ++j)
                    {
                        if (System.Math.Abs(diff[i, j]) > tolerance)
                        {
                            QAssert.Fail("Failed to reproduce integrated covariance"
                                         + "\n    calculated: " + diff[i, j]
                                         + "\n    expected:   " + 0);
                        }
                    }
                }
            }
        }

        [Fact(Skip = "LongRun")]
        public void testMonteCarloCapletPricing()
        {
            // Testing caplet LMM Monte-Carlo caplet pricing

            /* factor loadings are taken from Hull & White article
               plus extra normalisation to get orthogonal eigenvectors
               http://www.rotman.utoronto.ca/~amackay/fin/libormktmodel2.pdf */
            double[] compValues = {0.85549771, 0.46707264, 0.22353259,
                                0.91915359, 0.37716089, 0.11360610,
                                0.96438280, 0.26413316, -0.01412414,
                                0.97939148, 0.13492952, -0.15028753,
                                0.95970595, -0.00000000, -0.28100621,
                                0.97939148, -0.13492952, -0.15028753,
                                0.96438280, -0.26413316, -0.01412414,
                                0.91915359, -0.37716089, 0.11360610,
                                0.85549771, -0.46707264, 0.22353259
                               };

            var volaComp = new Matrix(9, 3);
            List<double> lcompValues = new InitializedList<double>(27, 0);
            List<double> ltemp = new InitializedList<double>(3, 0);
            lcompValues = compValues.ToList();
            //std::copy(compValues, compValues+9*3, volaComp.begin());
            for (var i = 0; i < 9; i++)
            {
                ltemp = lcompValues.GetRange(3 * i, 3);
                for (var j = 0; j < 3; j++)
                {
                    volaComp[i, j] = ltemp[j];
                }
            }
            var process1 = makeProcess();
            var process2 = makeProcess(volaComp);

            var tmp = process1.fixingTimes();
            var grid = new TimeGrid(tmp, tmp.Count, 12);

            var location = new List<int>();
            for (var i = 0; i < tmp.Count; ++i)
            {
                location.Add(grid.index(tmp[i]));
            }

            // set-up a small Monte-Carlo simulation to price caplets
            // and ratchet caps using a one- and a three factor libor market model

            ulong seed = 42;
            LowDiscrepancy.icInstance = new InverseCumulativeNormal();
            var rsg1 = new LowDiscrepancy().make_sequence_generator(
                           process1.factors() * (grid.size() - 1), seed);
            var rsg2 = new LowDiscrepancy().make_sequence_generator(
                           process2.factors() * (grid.size() - 1), seed);

            var generator1 = new MultiPathGenerator<IRNG>(process1, grid, rsg1, false);
            var generator2 = new MultiPathGenerator<IRNG>(process2, grid, rsg2, false);

            const int nrTrails = 250000;
            List<GeneralStatistics> stat1 = new InitializedList<GeneralStatistics>(process1.size());
            List<GeneralStatistics> stat2 = new InitializedList<GeneralStatistics>(process2.size());
            List<GeneralStatistics> stat3 = new InitializedList<GeneralStatistics>(process2.size() - 1);
            for (var i = 0; i < nrTrails; ++i)
            {
                var path1 = generator1.next();
                var path2 = generator2.next();
                var value1 = path1.value as MultiPath;
                QLNet.Utils.QL_REQUIRE(value1 != null, () => "Invalid Path");
                var value2 = path2.value as MultiPath;
                QLNet.Utils.QL_REQUIRE(value2 != null, () => "Invalid Path");

                List<double> rates1 = new InitializedList<double>(len);
                List<double> rates2 = new InitializedList<double>(len);
                for (var j = 0; j < process1.size(); ++j)
                {
                    rates1[j] = value1[j][location[j]];
                    rates2[j] = value2[j][location[j]];
                }

                var dis1 = process1.discountBond(rates1);
                var dis2 = process2.discountBond(rates2);

                for (var k = 0; k < process1.size(); ++k)
                {
                    var accrualPeriod = process1.accrualEndTimes()[k]
                                        - process1.accrualStartTimes()[k];
                    // caplet payoff function, cap rate at 4%
                    var payoff1 = System.Math.Max(rates1[k] - 0.04, 0.0) * accrualPeriod;

                    var payoff2 = System.Math.Max(rates2[k] - 0.04, 0.0) * accrualPeriod;
                    stat1[k].add(dis1[k] * payoff1);
                    stat2[k].add(dis2[k] * payoff2);

                    if (k != 0)
                    {
                        // ratchet cap payoff function
                        var payoff3 = System.Math.Max(rates2[k] - (rates2[k - 1] + 0.0025), 0.0)
                                      * accrualPeriod;
                        stat3[k - 1].add(dis2[k] * payoff3);
                    }
                }

            }

            double[] capletNpv = {0.000000000000, 0.000002841629, 0.002533279333,
                               0.009577143571, 0.017746502618, 0.025216116835,
                               0.031608230268, 0.036645683881, 0.039792254012,
                               0.041829864365
                              };

            double[] ratchetNpv = {0.0082644895, 0.0082754754, 0.0082159966,
                                0.0082982822, 0.0083803357, 0.0084366961,
                                0.0084173270, 0.0081803406, 0.0079533814
                               };

            for (var k = 0; k < process1.size(); ++k)
            {

                var calculated1 = stat1[k].mean();
                var tolerance1 = stat1[k].errorEstimate();
                var expected = capletNpv[k];

                if (System.Math.Abs(calculated1 - expected) > tolerance1)
                {
                    QAssert.Fail("Failed to reproduce expected caplet NPV"
                                 + "\n    calculated: " + calculated1
                                 + "\n    error int:  " + tolerance1
                                 + "\n    expected:   " + expected);
                }

                var calculated2 = stat2[k].mean();
                var tolerance2 = stat2[k].errorEstimate();

                if (System.Math.Abs(calculated2 - expected) > tolerance2)
                {
                    QAssert.Fail("Failed to reproduce expected caplet NPV"
                                 + "\n    calculated: " + calculated2
                                 + "\n    error int:  " + tolerance2
                                 + "\n    expected:   " + expected);
                }

                if (k != 0)
                {
                    var calculated3 = stat3[k - 1].mean();
                    var tolerance3 = stat3[k - 1].errorEstimate();
                    expected = ratchetNpv[k - 1];

                    var refError = 1e-5; // 1e-5. error bars of the reference values

                    if (System.Math.Abs(calculated3 - expected) > tolerance3 + refError)
                    {
                        QAssert.Fail("Failed to reproduce expected caplet NPV"
                                     + "\n    calculated: " + calculated3
                                     + "\n    error int:  " + tolerance3 + refError
                                     + "\n    expected:   " + expected);
                    }
                }
            }
        }
    }

}
