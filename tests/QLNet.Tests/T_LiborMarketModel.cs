/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using Xunit;
using QLNet.Math.randomnumbers;
using QLNet.legacy.libormarketmodels;
using QLNet.Methods.montecarlo;
using QLNet.Models;
using QLNet.Time;
using QLNet.Math.Optimization;
using QLNet.Instruments;
using QLNet.Pricingengines.CapFloor;
using QLNet.Math.Distributions;
using QLNet.Indexes;
using QLNet.Math.statistics;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Math.Interpolations;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Models.Shortrate.calibrationhelpers;
using QLNet.Pricingengines.Swap;
using QLNet.Quotes;
using QLNet.Termstructures.Yield;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_LiborMarketModel : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_LiborMarketModel()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        IborIndex makeIndex(List<Date> dates,
                            List<double> rates)
        {
            DayCounter dayCounter = new Actual360();

            var termStructure = new RelinkableHandle<YieldTermStructure>();
            IborIndex index = new Euribor6M(termStructure);

            var todaysDate =
               index.fixingCalendar().adjust(new Date(4, 9, 2005));
            Settings.setEvaluationDate(todaysDate);

            dates[0] = index.fixingCalendar().advance(todaysDate,
                                                      index.fixingDays(), TimeUnit.Days);
            var Interpolator = new Linear();
            termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            return index;
        }

        IborIndex makeIndex()
        {
            var dates = new List<Date>();
            var rates = new List<double>();
            dates.Add(new Date(4, 9, 2005));
            dates.Add(new Date(4, 9, 2018));
            rates.Add(0.039);
            rates.Add(0.041);

            return makeIndex(dates, rates);
        }

        OptionletVolatilityStructure makeCapVolCurve(Date todaysDate)
        {
            double[] vols = {14.40, 17.15, 16.81, 16.64, 16.17,
                          15.78, 15.40, 15.21, 14.86
                         };

            var dates = new List<Date>();
            var capletVols = new List<double>();
            var process =
               new LiborForwardModelProcess(10, makeIndex());

            for (var i = 0; i < 9; ++i)
            {
                capletVols.Add(vols[i] / 100);
                dates.Add(process.fixingDates()[i + 1]);
            }

            return new CapletVarianceCurve(todaysDate, dates,
                                           capletVols, new Actual360());
        }

        [Fact(Skip = "LongRun")]
        public void testSimpleCovarianceModels()
        {
            // Testing simple covariance models
            const int size = 10;
            const double tolerance = 1e-14;
            int i;

            LmCorrelationModel corrModel = new LmExponentialCorrelationModel(size, 0.1);

            var recon = corrModel.correlation(0.0, null)
                        - corrModel.pseudoSqrt(0.0, null) * Matrix.transpose(corrModel.pseudoSqrt(0.0, null));

            for (i = 0; i < size; ++i)
            {
                for (var j = 0; j < size; ++j)
                {
                    if (System.Math.Abs(recon[i, j]) > tolerance)
                        QAssert.Fail("Failed to reproduce correlation matrix"
                                     + "\n    calculated: " + recon[i, j]
                                     + "\n    expected:   " + 0);
                }
            }

            List<double> fixingTimes = new InitializedList<double>(size);
            for (i = 0; i < size; ++i)
            {
                fixingTimes[i] = 0.5 * i;
            }

            const double a = 0.2;
            const double b = 0.1;
            const double c = 2.1;
            const double d = 0.3;

            LmVolatilityModel volaModel = new LmLinearExponentialVolatilityModel(fixingTimes, a, b, c, d);

            var covarProxy = new LfmCovarianceProxy(volaModel, corrModel);

            var process = new LiborForwardModelProcess(size, makeIndex());

            var liborModel = new LiborForwardModel(process, volaModel, corrModel);

            for (double t = 0; t < 4.6; t += 0.31)
            {
                recon = covarProxy.covariance(t, null)
                        - covarProxy.diffusion(t, null) * Matrix.transpose(covarProxy.diffusion(t, null));

                for (var k = 0; k < size; ++k)
                {
                    for (var j = 0; j < size; ++j)
                    {
                        if (System.Math.Abs(recon[k, j]) > tolerance)
                            QAssert.Fail("Failed to reproduce correlation matrix"
                                         + "\n    calculated: " + recon[k, j]
                                         + "\n    expected:   " + 0);
                    }
                }

                var volatility = volaModel.volatility(t, null);

                for (var k = 0; k < size; ++k)
                {
                    double expected = 0;
                    if (k > 2 * t)
                    {
                        var T = fixingTimes[k];
                        expected = (a * (T - t) + d) * System.Math.Exp(-b * (T - t)) + c;
                    }

                    if (System.Math.Abs(expected - volatility[k]) > tolerance)
                        QAssert.Fail("Failed to reproduce volatities"
                                     + "\n    calculated: " + volatility[k]
                                     + "\n    expected:   " + expected);
                }
            }
        }

        [Fact(Skip = "LongRun")]
        public void testCapletPricing()
        {
            // Testing caplet pricing
            const int size = 10;
#if QL_USE_INDEXED_COUPON
         const double tolerance = 1e-5;
#else
            const double tolerance = 1e-12;
#endif

            var index = makeIndex();
            var process = new LiborForwardModelProcess(size, index);

            // set-up pricing engine
            var capVolCurve = makeCapVolCurve(Settings.evaluationDate());

            var variances = new LfmHullWhiteParameterization(process, capVolCurve).covariance(0.0, null).diagonal();

            LmVolatilityModel volaModel = new LmFixedVolatilityModel(Vector.Sqrt(variances), process.fixingTimes());

            LmCorrelationModel corrModel = new LmExponentialCorrelationModel(size, 0.3);

            IAffineModel model = new LiborForwardModel(process, volaModel, corrModel);

            var termStructure = process.index().forwardingTermStructure();

            var engine1 = new AnalyticCapFloorEngine(model, termStructure);

            var cap1 = new Cap(process.cashFlows(),
                               new InitializedList<double>(size, 0.04));
            cap1.setPricingEngine(engine1);

            const double expected = 0.015853935178;
            var calculated = cap1.NPV();

            if (System.Math.Abs(expected - calculated) > tolerance)
                QAssert.Fail("Failed to reproduce npv"
                             + "\n    calculated: " + calculated
                             + "\n    expected:   " + expected);
        }

        [Fact(Skip = "LongRun")]
        public void testCalibration()
        {
            // Testing calibration of a Libor forward model
            const int size = 14;
            const double tolerance = 8e-3;

            double[] capVols = {0.145708, 0.158465, 0.166248, 0.168672,
                             0.169007, 0.167956, 0.166261, 0.164239,
                             0.162082, 0.159923, 0.157781, 0.155745,
                             0.153776, 0.151950, 0.150189, 0.148582,
                             0.147034, 0.145598, 0.144248
                            };

            double[] swaptionVols = {0.170595, 0.166844, 0.158306, 0.147444,
                                  0.136930, 0.126833, 0.118135, 0.175963,
                                  0.166359, 0.155203, 0.143712, 0.132769,
                                  0.122947, 0.114310, 0.174455, 0.162265,
                                  0.150539, 0.138734, 0.128215, 0.118470,
                                  0.110540, 0.169780, 0.156860, 0.144821,
                                  0.133537, 0.123167, 0.114363, 0.106500,
                                  0.164521, 0.151223, 0.139670, 0.128632,
                                  0.119123, 0.110330, 0.103114, 0.158956,
                                  0.146036, 0.134555, 0.124393, 0.115038,
                                  0.106996, 0.100064
                                 };

            var index = makeIndex();
            var process = new LiborForwardModelProcess(size, index);
            var termStructure = index.forwardingTermStructure();

            // set-up the model
            LmVolatilityModel volaModel = new LmExtLinearExponentialVolModel(process.fixingTimes(),
                                                                             0.5, 0.6, 0.1, 0.1);

            LmCorrelationModel corrModel = new LmLinearExponentialCorrelationModel(size, 0.5, 0.8);

            var model = new LiborForwardModel(process, volaModel, corrModel);

            var swapVolIndex = 0;
            var dayCounter = index.forwardingTermStructure().link.dayCounter();

            // set-up calibration helper
            var calibrationHelper = new List<CalibrationHelper>();

            int i;
            for (i = 2; i < size; ++i)
            {
                var maturity = i * index.tenor();
                var capVol = new Handle<Quote>(new SimpleQuote(capVols[i - 2]));

                CalibrationHelper caphelper = new CapHelper(maturity, capVol, index, Frequency.Annual,
                                                            index.dayCounter(), true, termStructure, CalibrationHelper.CalibrationErrorType.ImpliedVolError);

                caphelper.setPricingEngine(new AnalyticCapFloorEngine(model, termStructure));

                calibrationHelper.Add(caphelper);

                if (i <= size / 2)
                {
                    // add a few swaptions to test swaption calibration as well
                    for (var j = 1; j <= size / 2; ++j)
                    {
                        var len = j * index.tenor();
                        var swaptionVol = new Handle<Quote>(
                           new SimpleQuote(swaptionVols[swapVolIndex++]));

                        CalibrationHelper swaptionHelper =
                           new SwaptionHelper(maturity, len, swaptionVol, index,
                                              index.tenor(), dayCounter,
                                              index.dayCounter(),
                                              termStructure, CalibrationHelper.CalibrationErrorType.ImpliedVolError);

                        swaptionHelper.setPricingEngine(new LfmSwaptionEngine(model, termStructure));

                        calibrationHelper.Add(swaptionHelper);
                    }
                }
            }

            var om = new LevenbergMarquardt(1e-6, 1e-6, 1e-6);
            //ConjugateGradient gc = new ConjugateGradient();

            model.calibrate(calibrationHelper,
                            om,
                            new EndCriteria(2000, 100, 1e-6, 1e-6, 1e-6),
                            new Constraint(),
                            new List<double>());

            // measure the calibration error
            var calculated = 0.0;
            for (i = 0; i < calibrationHelper.Count; ++i)
            {
                var diff = calibrationHelper[i].calibrationError();
                calculated += diff * diff;
            }

            if (System.Math.Sqrt(calculated) > tolerance)
                QAssert.Fail("Failed to calibrate libor forward model"
                             + "\n    calculated diff: " + System.Math.Sqrt(calculated)
                             + "\n    expected : smaller than  " + tolerance);
        }

        [Fact(Skip = "LongRun")]
        public void testSwaptionPricing()
        {
            // Testing forward swap and swaption pricing
            const int size = 10;
            const int steps = 8 * size;
#if QL_USE_INDEXED_COUPON
         const double tolerance = 1e-6;
#else
            const double tolerance = 1e-12;
#endif

            var dates = new List<Date>();
            var rates = new List<double>();
            dates.Add(new Date(4, 9, 2005));
            dates.Add(new Date(4, 9, 2011));
            rates.Add(0.04);
            rates.Add(0.08);

            var index = makeIndex(dates, rates);

            var process = new LiborForwardModelProcess(size, index);

            LmCorrelationModel corrModel = new LmExponentialCorrelationModel(size, 0.5);

            LmVolatilityModel volaModel = new LmLinearExponentialVolatilityModel(process.fixingTimes(),
                                                                                 0.291, 1.483, 0.116, 0.00001);

            // set-up pricing engine
            process.setCovarParam(
                                  new LfmCovarianceProxy(volaModel, corrModel));

            // set-up a small Monte-Carlo simulation to price swations
            var tmp = process.fixingTimes();

            var grid = new TimeGrid(tmp, tmp.Count, steps);

            var location = new List<int>();
            for (var i = 0; i < tmp.Count; ++i)
            {
                location.Add(grid.index(tmp[i]));
            }

            ulong seed = 42;
            const int nrTrails = 5000;
            LowDiscrepancy.icInstance = new InverseCumulativeNormal();

            IRNG rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                        , InverseCumulativeNormal>)
                       new PseudoRandom().make_sequence_generator(process.factors() * (grid.size() - 1), seed);



            var generator = new MultiPathGenerator<IRNG>(process,
                                                                              grid,
                                                                              rsg, false);

            var liborModel = new LiborForwardModel(process, volaModel, corrModel);

            var calendar = index.fixingCalendar();
            var dayCounter = index.forwardingTermStructure().link.dayCounter();
            var convention = index.businessDayConvention();

            var settlement = index.forwardingTermStructure().link.referenceDate();

            var m = liborModel.getSwaptionVolatilityMatrix();

            for (var i = 1; i < size; ++i)
            {
                for (var j = 1; j <= size - i; ++j)
                {
                    var fwdStart = settlement + new Period(6 * i, TimeUnit.Months);
                    var fwdMaturity = fwdStart + new Period(6 * j, TimeUnit.Months);

                    var schedule = new Schedule(fwdStart, fwdMaturity, index.tenor(), calendar,
                                                     convention, convention, DateGeneration.Rule.Forward, false);

                    var swapRate = 0.0404;
                    var forwardSwap = new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                                                              schedule, swapRate, dayCounter,
                                                              schedule, index, 0.0, index.dayCounter());
                    forwardSwap.setPricingEngine(new DiscountingSwapEngine(index.forwardingTermStructure()));

                    // check forward pricing first
                    var expected = forwardSwap.fairRate();
                    var calculated = liborModel.S_0(i - 1, i + j - 1);

                    if (System.Math.Abs(expected - calculated) > tolerance)
                        QAssert.Fail("Failed to reproduce fair forward swap rate"
                                     + "\n    calculated: " + calculated
                                     + "\n    expected:   " + expected);

                    swapRate = forwardSwap.fairRate();
                    forwardSwap =
                       new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                                       schedule, swapRate, dayCounter,
                                       schedule, index, 0.0, index.dayCounter());
                    forwardSwap.setPricingEngine(new DiscountingSwapEngine(index.forwardingTermStructure()));

                    if (i == j && i <= size / 2)
                    {
                        IPricingEngine engine =
                           new LfmSwaptionEngine(liborModel, index.forwardingTermStructure());
                        Exercise exercise =
                           new EuropeanExercise(process.fixingDates()[i]);

                        var swaption =
                           new Swaption(forwardSwap, exercise);
                        swaption.setPricingEngine(engine);

                        var stat = new GeneralStatistics();

                        for (var n = 0; n < nrTrails; ++n)
                        {
                            var path = n % 2 != 0 ? generator.antithetic()
                                                 : generator.next();
                            var value = path.value as MultiPath;
                            Utils.QL_REQUIRE(value != null, () => "Invalid Path");
                            //Sample<MultiPath> path = generator.next();
                            List<double> rates_ = new InitializedList<double>(size);
                            for (var k = 0; k < process.size(); ++k)
                            {
                                rates_[k] = value[k][location[i]];
                            }
                            var dis = process.discountBond(rates_);

                            var npv = 0.0;
                            for (var k = i; k < i + j; ++k)
                            {
                                npv += (swapRate - rates_[k])
                                       * (process.accrualEndTimes()[k]
                                          - process.accrualStartTimes()[k]) * dis[k];
                            }
                            stat.add(System.Math.Max(npv, 0.0));
                        }

                        if (System.Math.Abs(swaption.NPV() - stat.mean())
                            > stat.errorEstimate() * 2.35)
                            QAssert.Fail("Failed to reproduce swaption npv"
                                         + "\n    calculated: " + stat.mean()
                                         + "\n    expected:   " + swaption.NPV());
                    }
                }
            }
        }
    }
}
