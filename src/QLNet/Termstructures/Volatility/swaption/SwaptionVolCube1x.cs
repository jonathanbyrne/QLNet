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
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Math.Optimization;
using QLNet.Patterns;
using QLNet.Processes;
using QLNet.Quotes;
using QLNet.Time;

/* Swaption volatility cube, fit-early-interpolate-later approach
   The provided types are
   SwaptionVolCube1 using the classic Hagan 2002 Sabr formula
   SwaptionVolCube1a using the No Arbitrage Sabr model (Doust)
*/

namespace QLNet.Termstructures.Volatility.swaption
{
    [PublicAPI]
    public class SwaptionVolCube1x : SwaptionVolatilityCube
    {
        public delegate SABRInterpolation GetInterpolation(GeneralizedBlackScholesProcess process);

        [PublicAPI]
        public class Cube
        {
            private bool backwardFlat_;
            private bool extrapolation_;
            private List<Interpolation2D> interpolators_;
            private int nLayers_;
            private List<Date> optionDates_;
            private List<double> optionTimes_, swapLengths_;
            private List<Matrix> points_;
            private List<Period> swapTenors_;
            private List<Matrix> transposedPoints_;

            public Cube()
            {
            }

            public Cube(List<Date> optionDates,
                List<Period> swapTenors,
                List<double> optionTimes,
                List<double> swapLengths,
                int nLayers,
                bool extrapolation = true,
                bool backwardFlat = false)
            {
                optionTimes_ = new List<double>(optionTimes);
                swapLengths_ = new List<double>(swapLengths);
                optionDates_ = new List<Date>(optionDates);
                swapTenors_ = new List<Period>(swapTenors);
                nLayers_ = nLayers;
                extrapolation_ = extrapolation;
                backwardFlat_ = backwardFlat;
                interpolators_ = new List<Interpolation2D>();
                transposedPoints_ = new List<Matrix>();

                QLNet.Utils.QL_REQUIRE(optionTimes.Count > 1, () => "Cube::Cube(...): optionTimes.size()<2");
                QLNet.Utils.QL_REQUIRE(swapLengths.Count > 1, () => "Cube::Cube(...): swapLengths.size()<2");

                QLNet.Utils.QL_REQUIRE(optionTimes.Count == optionDates.Count, () => "Cube::Cube(...): optionTimes/optionDates mismatch");
                QLNet.Utils.QL_REQUIRE(swapTenors.Count == swapLengths.Count, () => "Cube::Cube(...): swapTenors/swapLengths mismatch");

                List<Matrix> points = new InitializedList<Matrix>(nLayers_);
                for (var i = 0; i < nLayers; i++)
                {
                    points[i] = new Matrix(optionTimes_.Count, swapLengths_.Count, 0.0);
                }

                for (var k = 0; k < nLayers_; k++)
                {
                    Interpolation2D interpolation;
                    transposedPoints_.Add(Matrix.transpose(points[k]));
                    if (k <= 4 && backwardFlat_)
                    {
                        interpolation = new BackwardflatLinearInterpolation(optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count, transposedPoints_[k]);
                    }
                    else
                    {
                        interpolation = new BilinearInterpolation(optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count, transposedPoints_[k]);
                    }

                    interpolators_.Add(new FlatExtrapolator2D(interpolation));
                    interpolators_[k].enableExtrapolation();
                }

                setPoints(points);
            }

            public Cube(Cube o)
            {
                optionTimes_ = o.optionTimes_;
                swapLengths_ = o.swapLengths_;
                optionDates_ = o.optionDates_;
                swapTenors_ = o.swapTenors_;
                nLayers_ = o.nLayers_;
                extrapolation_ = o.extrapolation_;
                backwardFlat_ = o.backwardFlat_;
                transposedPoints_ = o.transposedPoints_;
                for (var k = 0; k < nLayers_; ++k)
                {
                    Interpolation2D interpolation;
                    if (k <= 4 && backwardFlat_)
                    {
                        interpolation = new BackwardflatLinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }
                    else
                    {
                        interpolation = new BilinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }

                    interpolators_.Add(new FlatExtrapolator2D(interpolation));
                    interpolators_[k].enableExtrapolation();
                }

                setPoints(o.points_);
            }

            public Matrix browse()
            {
                var result = new Matrix(swapLengths_.Count * optionTimes_.Count, nLayers_ + 2, 0.0);
                for (var i = 0; i < swapLengths_.Count; ++i)
                {
                    for (var j = 0; j < optionTimes_.Count; ++j)
                    {
                        result[i * optionTimes_.Count + j, 0] = swapLengths_[i];
                        result[i * optionTimes_.Count + j, 1] = optionTimes_[j];
                        for (var k = 0; k < nLayers_; ++k)
                        {
                            var p = points_[k];
                            result[i * optionTimes_.Count + j, 2 + k] = p[j, i];
                        }
                    }
                }

                return result;
            }

            public Cube clone(Cube o)
            {
                optionTimes_ = o.optionTimes_;
                swapLengths_ = o.swapLengths_;
                optionDates_ = o.optionDates_;
                swapTenors_ = o.swapTenors_;
                nLayers_ = o.nLayers_;
                extrapolation_ = o.extrapolation_;
                backwardFlat_ = o.backwardFlat_;
                transposedPoints_ = o.transposedPoints_;
                for (var k = 0; k < nLayers_; k++)
                {
                    Interpolation2D interpolation;
                    if (k <= 4 && backwardFlat_)
                    {
                        interpolation = new BackwardflatLinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }
                    else
                    {
                        interpolation = new BilinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }

                    interpolators_.Add(new FlatExtrapolator2D(interpolation));
                    interpolators_[k].enableExtrapolation();
                }

                setPoints(o.points_);
                return this;
            }

            public void expandLayers(int i, bool expandOptionTimes, int j, bool expandSwapLengths)
            {
                QLNet.Utils.QL_REQUIRE(i <= optionTimes_.Count, () => "Cube::expandLayers: incompatible size 1");
                QLNet.Utils.QL_REQUIRE(j <= swapLengths_.Count, () => "Cube::expandLayers: incompatible size 2");

                if (expandOptionTimes)
                {
                    optionTimes_.Insert(i, 0.0);
                    optionDates_.Insert(i, new Date());
                }

                if (expandSwapLengths)
                {
                    swapLengths_.Insert(j, 0.0);
                    swapTenors_.Insert(j, new Period());
                }

                List<Matrix> newPoints = new InitializedList<Matrix>(nLayers_);
                for (var ii = 0; ii < nLayers_; ii++)
                {
                    newPoints[ii]
                        = new Matrix(optionTimes_.Count, swapLengths_.Count, 0.0);
                }

                for (var k = 0; k < nLayers_; ++k)
                {
                    for (var u = 0; u < points_[k].rows(); ++u)
                    {
                        var indexOfRow = u;
                        if (u >= i && expandOptionTimes)
                        {
                            indexOfRow = u + 1;
                        }

                        for (var v = 0; v < points_[k].columns(); ++v)
                        {
                            var indexOfCol = v;
                            if (v >= j && expandSwapLengths)
                            {
                                indexOfCol = v + 1;
                            }

                            Matrix p = newPoints[k], p1 = points_[k];
                            p[indexOfRow, indexOfCol] = p1[u, v];
                        }
                    }
                }

                setPoints(newPoints);
            }

            public List<Date> optionDates() => optionDates_;

            public List<double> optionTimes() => optionTimes_;

            public List<Matrix> points() => points_;

            public void setElement(int IndexOfLayer, int IndexOfRow, int IndexOfColumn, double x)
            {
                QLNet.Utils.QL_REQUIRE(IndexOfLayer < nLayers_, () => "Cube::setElement: incompatible IndexOfLayer ");
                QLNet.Utils.QL_REQUIRE(IndexOfRow < optionTimes_.Count, () => "Cube::setElement: incompatible IndexOfRow");
                QLNet.Utils.QL_REQUIRE(IndexOfColumn < swapLengths_.Count, () => "Cube::setElement: incompatible IndexOfColumn");
                var p = points_[IndexOfLayer];
                p[IndexOfRow, IndexOfColumn] = x;
            }

            public void setLayer(int i, Matrix x)
            {
                QLNet.Utils.QL_REQUIRE(i < nLayers_, () => "Cube::setLayer: incompatible number of layer ");
                QLNet.Utils.QL_REQUIRE(x.rows() == optionTimes_.Count, () => "Cube::setLayer: incompatible size 1");
                QLNet.Utils.QL_REQUIRE(x.columns() == swapLengths_.Count, () => "Cube::setLayer: incompatible size 2");

                points_[i] = x;
            }

            public void setPoint(Date optionDate, Period swapTenor, double optionTime, double swapLength, List<double> point)
            {
                var expandOptionTimes = !optionTimes_.Exists(x => x.IsEqual(optionTime));
                var expandSwapLengths = !swapLengths_.Exists(x => x.IsEqual(swapLength));

                double optionTimesPreviousNode, swapLengthsPreviousNode;

                optionTimesPreviousNode = optionTimes_.First(x => x >= System.Math.Min(optionTime, optionTimes_.Max()));
                var optionTimesIndex = optionTimes_.IndexOf(optionTimesPreviousNode);

                swapLengthsPreviousNode = swapLengths_.First(x => x >= System.Math.Min(swapLength, swapLengths_.Max()));
                var swapLengthsIndex = swapLengths_.IndexOf(swapLengthsPreviousNode);

                if (expandOptionTimes || expandSwapLengths)
                {
                    expandLayers(optionTimesIndex, expandOptionTimes, swapLengthsIndex, expandSwapLengths);
                }

                for (var k = 0; k < nLayers_; ++k)
                {
                    var p = points_[k];
                    p[optionTimesIndex, swapLengthsIndex] = point[k];
                }

                optionTimes_[optionTimesIndex] = optionTime;
                swapLengths_[swapLengthsIndex] = swapLength;
                optionDates_[optionTimesIndex] = optionDate;
                swapTenors_[swapLengthsIndex] = swapTenor;
            }

            public void setPoints(List<Matrix> x)
            {
                QLNet.Utils.QL_REQUIRE(x.Count == nLayers_, () => "Cube::setPoints: incompatible number of layers ");
                QLNet.Utils.QL_REQUIRE(x[0].rows() == optionTimes_.Count, () => "Cube::setPoints: incompatible size 1");
                QLNet.Utils.QL_REQUIRE(x[0].columns() == swapLengths_.Count, () => "Cube::setPoints: incompatible size 2");

                points_ = x;
            }

            public List<double> swapLengths() => swapLengths_;

            public List<Period> swapTenors() => swapTenors_;

            public void updateInterpolators()
            {
                for (var k = 0; k < nLayers_; ++k)
                {
                    transposedPoints_[k] = Matrix.transpose(points_[k]);
                    Interpolation2D interpolation;
                    if (k <= 4 && backwardFlat_)
                    {
                        interpolation = new BackwardflatLinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }
                    else
                    {
                        interpolation = new BilinearInterpolation(
                            optionTimes_, optionTimes_.Count,
                            swapLengths_, swapLengths_.Count,
                            transposedPoints_[k]);
                    }

                    interpolators_[k] = new FlatExtrapolator2D(interpolation);
                    interpolators_[k].enableExtrapolation();
                }
            }

            public List<double> value(double optionTime, double swapLength)
            {
                var result = new List<double>();
                for (var k = 0; k < nLayers_; ++k)
                {
                    result.Add(interpolators_[k].value(optionTime, swapLength));
                }

                return result;
            }
        }

        private class PrivateObserver : IObserver
        {
            private readonly SwaptionVolCube1x v_;

            public PrivateObserver(SwaptionVolCube1x v)
            {
                v_ = v;
            }

            public void update()
            {
                v_.setParameterGuess();
                v_.update();
            }
        }

        private const double SWAPTIONVOLCUBE_TOL = 100.0e-4;
        private const double SWAPTIONVOLCUBE_VEGAWEIGHTED_TOL = 15.0e-4;
        private bool backwardFlat_;
        private double cutoffStrike_;
        private Cube denseParameters_;
        private EndCriteria endCriteria_;
        private double errorAccept_;
        private bool isAtmCalibrated_;
        private List<bool> isParameterFixed_;
        private Cube marketVolCube_;
        private double maxErrorTolerance_;
        private int maxGuesses_;
        private OptimizationMethod optMethod_;
        private Cube parametersGuess_;
        private List<List<Handle<Quote>>> parametersGuessQuotes_;
        private PrivateObserver privateObserver_;
        private Cube sparseParameters_;
        private List<List<SmileSection>> sparseSmiles_;
        private bool useMaxError_;
        private Cube volCubeAtmCalibrated_;

        public SwaptionVolCube1x(Handle<SwaptionVolatilityStructure> atmVolStructure,
            List<Period> optionTenors,
            List<Period> swapTenors,
            List<double> strikeSpreads,
            List<List<Handle<Quote>>> volSpreads,
            SwapIndex swapIndexBase,
            SwapIndex shortSwapIndexBase,
            bool vegaWeightedSmileFit,
            List<List<Handle<Quote>>> parametersGuess,
            List<bool> isParameterFixed,
            bool isAtmCalibrated,
            EndCriteria endCriteria = null,
            double? maxErrorTolerance = null,
            OptimizationMethod optMethod = null,
            double? errorAccept = null,
            bool useMaxError = false,
            int maxGuesses = 50,
            bool backwardFlat = false,
            double cutoffStrike = 0.0001)
            : base(atmVolStructure, optionTenors, swapTenors, strikeSpreads, volSpreads, swapIndexBase,
                shortSwapIndexBase, vegaWeightedSmileFit)
        {
            parametersGuessQuotes_ = parametersGuess;
            isParameterFixed_ = isParameterFixed;
            isAtmCalibrated_ = isAtmCalibrated;
            endCriteria_ = endCriteria;
            optMethod_ = optMethod;
            useMaxError_ = useMaxError;
            maxGuesses_ = maxGuesses;
            backwardFlat_ = backwardFlat;
            cutoffStrike_ = cutoffStrike;

            if (maxErrorTolerance != null)
            {
                maxErrorTolerance_ = maxErrorTolerance.Value;
            }
            else
            {
                maxErrorTolerance_ = SWAPTIONVOLCUBE_TOL;
                if (vegaWeightedSmileFit_)
                {
                    maxErrorTolerance_ = SWAPTIONVOLCUBE_VEGAWEIGHTED_TOL;
                }
            }

            if (errorAccept != null)
            {
                errorAccept_ = errorAccept.Value;
            }
            else
            {
                errorAccept_ = maxErrorTolerance_ / 5.0;
            }

            privateObserver_ = new PrivateObserver(this);
            registerWithParametersGuess();
            setParameterGuess();
        }

        public Matrix denseSabrParameters()
        {
            calculate();
            return denseParameters_.browse();
        }

        // Other inspectors
        public Matrix marketVolCube(int i) => marketVolCube_.points()[i];

        public Matrix marketVolCube()
        {
            calculate();
            return marketVolCube_.browse();
        }

        public void recalibration(double beta, Period swapTenor)
        {
            List<double> betaVector = new InitializedList<double>(nOptionTenors_, beta);
            recalibration(betaVector, swapTenor);
        }

        public void recalibration(List<double> beta, Period swapTenor)
        {
            QLNet.Utils.QL_REQUIRE(beta.Count == nOptionTenors_, () =>
                "beta size (" + beta.Count + ") must be equal to number of option tenors (" + nOptionTenors_ + ")");

            var swapTenors = marketVolCube_.swapTenors();
            var k = swapTenors.IndexOf(swapTenors.First(x => x == swapTenor));

            QLNet.Utils.QL_REQUIRE(k != swapTenors.Count, () => "swap tenor (" + swapTenor + ") not found");

            for (var i = 0; i < nOptionTenors_; ++i)
            {
                parametersGuess_.setElement(1, i, k, beta[i]);
            }

            parametersGuess_.updateInterpolators();
            sabrCalibrationSection(marketVolCube_, sparseParameters_, swapTenor);

            volCubeAtmCalibrated_ = marketVolCube_;
            if (isAtmCalibrated_)
            {
                fillVolatilityCube();
                sabrCalibrationSection(volCubeAtmCalibrated_, denseParameters_, swapTenor);
            }

            notifyObservers();
        }

        public void recalibration(List<Period> swapLengths, List<double> beta, Period swapTenor)
        {
            QLNet.Utils.QL_REQUIRE(beta.Count == swapLengths.Count, () =>
                "beta size (" + beta.Count + ") must be equal to number of swap lenghts ("
                + swapLengths.Count + ")");

            var betaTimes = new List<double>();
            for (var i = 0; i < beta.Count; i++)
            {
                betaTimes.Add(timeFromReference(optionDateFromTenor(swapLengths[i])));
            }

            var betaInterpolation = new LinearInterpolation(betaTimes, betaTimes.Count, beta);

            var cubeBeta = new List<double>();
            for (var i = 0; i < optionTimes().Count; i++)
            {
                var t = optionTimes()[i];
                // flat extrapolation ensures admissable values
                if (t < betaTimes.First())
                {
                    t = betaTimes.First();
                }

                if (t > betaTimes.Last())
                {
                    t = betaTimes.Last();
                }

                cubeBeta.Add(betaInterpolation.value(t));
            }

            recalibration(cubeBeta, swapTenor);
        }

        public void sabrCalibrationSection(Cube marketVolCube, Cube parametersCube, Period swapTenor)
        {
            var optionTimes = marketVolCube.optionTimes();
            var swapLengths = marketVolCube.swapLengths();
            var optionDates = marketVolCube.optionDates();
            var swapTenors = marketVolCube.swapTenors();

            var k = swapTenors.IndexOf(swapTenors.First(x => x == swapTenor));

            QLNet.Utils.QL_REQUIRE(k != swapTenors.Count, () => "swap tenor not found");

            List<double> calibrationResult = new InitializedList<double>(8, 0.0);
            var tmpMarketVolCube = marketVolCube.points();

            var strikes = new List<double>(strikeSpreads_.Count);
            var volatilities = new List<double>(strikeSpreads_.Count);

            for (var j = 0; j < optionTimes.Count; j++)
            {
                var atmForward = atmStrike(optionDates[j], swapTenors[k]);
                var shiftTmp = atmVol_.link.shift(optionTimes[j], swapLengths[k]);
                strikes.Clear();
                volatilities.Clear();
                for (var i = 0; i < nStrikes_; i++)
                {
                    var strike = atmForward + strikeSpreads_[i];
                    if (strike + shiftTmp >= cutoffStrike_)
                    {
                        strikes.Add(strike);
                        volatilities.Add(tmpMarketVolCube[i][j, k]);
                    }
                }

                var guess = parametersGuess_.value(optionTimes[j], swapLengths[k]);

                var sabrInterpolation = new SABRInterpolation(strikes,
                    strikes.Count,
                    volatilities,
                    optionTimes[j], atmForward,
                    guess[0], guess[1],
                    guess[2], guess[3],
                    isParameterFixed_[0],
                    isParameterFixed_[1],
                    isParameterFixed_[2],
                    isParameterFixed_[3],
                    vegaWeightedSmileFit_,
                    endCriteria_,
                    optMethod_,
                    errorAccept_,
                    useMaxError_,
                    maxGuesses_,
                    shiftTmp,
                    volatilityType()); //shiftTmp

                sabrInterpolation.update();
                var interpolationError = sabrInterpolation.rmsError();
                calibrationResult[0] = sabrInterpolation.alpha();
                calibrationResult[1] = sabrInterpolation.beta();
                calibrationResult[2] = sabrInterpolation.nu();
                calibrationResult[3] = sabrInterpolation.rho();
                calibrationResult[4] = atmForward;
                calibrationResult[5] = interpolationError;
                calibrationResult[6] = sabrInterpolation.maxError();
                calibrationResult[7] = (double)sabrInterpolation.endCriteria();

                QLNet.Utils.QL_REQUIRE(calibrationResult[7].IsNotEqual((double)EndCriteria.Type.MaxIterations), () =>
                    "section calibration failed: " +
                    "option tenor " + optionDates[j] +
                    ", swap tenor " + swapTenors[k] +
                    ": max iteration (" +
                    endCriteria_.maxIterations() + ")" +
                    ", alpha " + calibrationResult[0] +
                    ", beta " + calibrationResult[1] +
                    ", nu " + calibrationResult[2] +
                    ", rho " + calibrationResult[3] +
                    ", max error " + calibrationResult[6] +
                    ", error " + calibrationResult[5]
                );

                QLNet.Utils.QL_REQUIRE(useMaxError_ ? calibrationResult[6] > 0 : calibrationResult[5] < maxErrorTolerance_, () =>
                    "section calibration failed: " +
                    "option tenor " + optionDates[j] +
                    ", swap tenor " + swapTenors[k] +
                    (useMaxError_ ? ": max error " : ": error ") +
                    (useMaxError_ ? calibrationResult[6] : calibrationResult[5]) +
                    ", alpha " + calibrationResult[0] +
                    ", beta " + calibrationResult[1] +
                    ", nu " + calibrationResult[2] +
                    ", rho " + calibrationResult[3] +
                    (useMaxError_ ? ": error" : ": max error ") +
                    (useMaxError_ ? calibrationResult[5] : calibrationResult[6])
                );

                parametersCube.setPoint(optionDates[j], swapTenors[k], optionTimes[j], swapLengths[k], calibrationResult);
                parametersCube.updateInterpolators();
            }
        }

        public Matrix sparseSabrParameters()
        {
            calculate();
            return sparseParameters_.browse();
        }

        public void updateAfterRecalibration()
        {
            volCubeAtmCalibrated_ = marketVolCube_;
            if (isAtmCalibrated_)
            {
                fillVolatilityCube();
                denseParameters_ = sabrCalibration(volCubeAtmCalibrated_);
                denseParameters_.updateInterpolators();
            }

            notifyObservers();
        }

        public Matrix volCubeAtmCalibrated()
        {
            calculate();
            return volCubeAtmCalibrated_.browse();
        }

        protected void createSparseSmiles()
        {
            var optionTimes = new List<double>(sparseParameters_.optionTimes());
            var swapLengths = new List<double>(sparseParameters_.swapLengths());
            if (sparseSmiles_ == null)
            {
                sparseSmiles_ = new List<List<SmileSection>>();
            }

            sparseSmiles_.Clear();

            for (var j = 0; j < optionTimes.Count; j++)
            {
                List<SmileSection> tmp;
                var n = swapLengths.Count;
                tmp = new List<SmileSection>(n);
                for (var k = 0; k < n; ++k)
                {
                    tmp.Add(smileSection(optionTimes[j], swapLengths[k], ref sparseParameters_));
                }

                sparseSmiles_.Add(tmp);
            }
        }

        protected void fillVolatilityCube()
        {
            var atmVolStructure = atmVol_.currentLink() as SwaptionVolatilityDiscrete;

            var atmOptionTimes = new List<double>(atmVolStructure.optionTimes());
            var optionTimes = new List<double>(volCubeAtmCalibrated_.optionTimes());
            atmOptionTimes.InsertRange(atmOptionTimes.Count, optionTimes);
            atmOptionTimes.Sort();
            atmOptionTimes = atmOptionTimes.Distinct().ToList();

            var atmSwapLengths = new List<double>(atmVolStructure.swapLengths());
            var swapLengths = new List<double>(volCubeAtmCalibrated_.swapLengths());
            atmSwapLengths.InsertRange(atmSwapLengths.Count, swapLengths);
            atmSwapLengths.Sort();
            atmSwapLengths = atmSwapLengths.Distinct().ToList();

            var atmOptionDates = new List<Date>(atmVolStructure.optionDates());
            var optionDates = new List<Date>(volCubeAtmCalibrated_.optionDates());
            atmOptionDates.InsertRange(atmOptionDates.Count, optionDates);
            atmOptionDates.Sort();
            atmOptionDates = atmOptionDates.Distinct().ToList();

            var atmSwapTenors = new List<Period>(atmVolStructure.swapTenors());
            var swapTenors = new List<Period>(volCubeAtmCalibrated_.swapTenors());
            atmSwapTenors.InsertRange(atmSwapTenors.Count, swapTenors);
            atmSwapTenors.Sort();
            atmSwapTenors = atmSwapTenors.Distinct().ToList();

            createSparseSmiles();

            for (var j = 0; j < atmOptionTimes.Count; j++)
            {
                for (var k = 0; k < atmSwapLengths.Count; k++)
                {
                    var expandOptionTimes = !optionTimes.Exists(x => x.IsEqual(atmOptionTimes[j]));
                    var expandSwapLengths = !swapLengths.Exists(x => x.IsEqual(atmSwapLengths[k]));
                    if (expandOptionTimes || expandSwapLengths)
                    {
                        var atmForward = atmStrike(atmOptionDates[j], atmSwapTenors[k]);
                        var atmVol = atmVol_.link.volatility(atmOptionDates[j], atmSwapTenors[k], atmForward);
                        var spreadVols = spreadVolInterpolation(atmOptionDates[j], atmSwapTenors[k]);
                        var volAtmCalibrated = new List<double>(nStrikes_);
                        for (var i = 0; i < nStrikes_; i++)
                        {
                            volAtmCalibrated.Add(atmVol + spreadVols[i]);
                        }

                        volCubeAtmCalibrated_.setPoint(atmOptionDates[j], atmSwapTenors[k],
                            atmOptionTimes[j], atmSwapLengths[k],
                            volAtmCalibrated);
                    }
                }
            }

            volCubeAtmCalibrated_.updateInterpolators();
        }

        // LazyObject interface
        protected override void performCalculations()
        {
            base.performCalculations();

            //! set marketVolCube_ by volSpreads_ quotes
            marketVolCube_ = new Cube(optionDates_, swapTenors_, optionTimes_, swapLengths_, nStrikes_);
            double atmForward;
            double atmVol, vol;
            for (var j = 0; j < nOptionTenors_; ++j)
            {
                for (var k = 0; k < nSwapTenors_; ++k)
                {
                    atmForward = atmStrike(optionDates_[j], swapTenors_[k]);
                    atmVol = atmVol_.link.volatility(optionDates_[j], swapTenors_[k], atmForward);
                    for (var i = 0; i < nStrikes_; ++i)
                    {
                        vol = atmVol + volSpreads_[j * nSwapTenors_ + k][i].link.value();
                        marketVolCube_.setElement(i, j, k, vol);
                    }
                }
            }

            marketVolCube_.updateInterpolators();

            sparseParameters_ = sabrCalibration(marketVolCube_);
            sparseParameters_.updateInterpolators();
            volCubeAtmCalibrated_ = marketVolCube_;

            if (isAtmCalibrated_)
            {
                fillVolatilityCube();
                denseParameters_ = sabrCalibration(volCubeAtmCalibrated_);
                denseParameters_.updateInterpolators();
            }
        }

        protected void registerWithParametersGuess()
        {
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < nOptionTenors_; j++)
            for (var k = 0; k < nSwapTenors_; k++)
            {
                parametersGuessQuotes_[j + k * nOptionTenors_][i].registerWith(privateObserver_.update);
            }
        }

        protected override int requiredNumberOfStrikes() => 1;

        protected Cube sabrCalibration(Cube marketVolCube)
        {
            var optionTimes = marketVolCube.optionTimes();
            var swapLengths = marketVolCube.swapLengths();
            var optionDates = marketVolCube.optionDates();
            var swapTenors = marketVolCube.swapTenors();
            var alphas = new Matrix(optionTimes.Count, swapLengths.Count, 0.0);
            var betas = new Matrix(alphas);
            var nus = new Matrix(alphas);
            var rhos = new Matrix(alphas);
            var forwards = new Matrix(alphas);
            var errors = new Matrix(alphas);
            var maxErrors = new Matrix(alphas);
            var endCriteria = new Matrix(alphas);

            var tmpMarketVolCube = marketVolCube.points();

            List<double> strikes = new InitializedList<double>(strikeSpreads_.Count);
            List<double> volatilities = new InitializedList<double>(strikeSpreads_.Count);

            for (var j = 0; j < optionTimes.Count; j++)
            {
                for (var k = 0; k < swapLengths.Count; k++)
                {
                    var atmForward = atmStrike(optionDates[j], swapTenors[k]);
                    var shiftTmp = atmVol_.link.shift(optionTimes[j], swapLengths[k]);
                    strikes.Clear();
                    volatilities.Clear();
                    for (var i = 0; i < nStrikes_; i++)
                    {
                        var strike = atmForward + strikeSpreads_[i];
                        if (strike + shiftTmp >= cutoffStrike_)
                        {
                            strikes.Add(strike);
                            var matrix = tmpMarketVolCube[i];
                            volatilities.Add(matrix[j, k]);
                        }
                    }

                    var guess = parametersGuess_.value(optionTimes[j], swapLengths[k]);

                    var sabrInterpolation = new SABRInterpolation(strikes, strikes.Count,
                        volatilities,
                        optionTimes[j], atmForward,
                        guess[0], guess[1],
                        guess[2], guess[3],
                        isParameterFixed_[0],
                        isParameterFixed_[1],
                        isParameterFixed_[2],
                        isParameterFixed_[3],
                        vegaWeightedSmileFit_,
                        endCriteria_,
                        optMethod_,
                        errorAccept_,
                        useMaxError_,
                        maxGuesses_,
                        shiftTmp,
                        volatilityType()); // shiftTmp
                    sabrInterpolation.update();

                    var rmsError = sabrInterpolation.rmsError();
                    var maxError = sabrInterpolation.maxError();
                    alphas[j, k] = sabrInterpolation.alpha();
                    betas[j, k] = sabrInterpolation.beta();
                    nus[j, k] = sabrInterpolation.nu();
                    rhos[j, k] = sabrInterpolation.rho();
                    forwards[j, k] = atmForward;
                    errors[j, k] = rmsError;
                    maxErrors[j, k] = maxError;
                    endCriteria[j, k] = (double)sabrInterpolation.endCriteria();

                    QLNet.Utils.QL_REQUIRE(endCriteria[j, k].IsNotEqual((double)EndCriteria.Type.MaxIterations), () =>
                        "global swaptions calibration failed: " +
                        "MaxIterations reached: " + "\n" +
                        "option maturity = " + optionDates[j] + ", \n" +
                        "swap tenor = " + swapTenors[k] + ", \n" +
                        "error = " + errors[j, k] + ", \n" +
                        "max error = " + maxErrors[j, k] + ", \n" +
                        "   alpha = " + alphas[j, k] + "n" +
                        "   beta = " + betas[j, k] + "\n" +
                        "   nu = " + nus[j, k] + "\n" +
                        "   rho = " + rhos[j, k] + "\n"
                    );

                    QLNet.Utils.QL_REQUIRE(useMaxError_ ? maxError > 0 : rmsError < maxErrorTolerance_, () =>
                        "global swaptions calibration failed: " +
                        "option tenor " + optionDates[j] +
                        ", swap tenor " + swapTenors[k] +
                        (useMaxError_ ? ": max error " : ": error") +
                        (useMaxError_ ? maxError : rmsError) +
                        "   alpha = " + alphas[j, k] + "\n" +
                        "   beta = " + betas[j, k] + "\n" +
                        "   nu = " + nus[j, k] + "\n" +
                        "   rho = " + rhos[j, k] + "\n" +
                        (useMaxError_ ? ": error" : ": max error ") +
                        (useMaxError_ ? rmsError : maxError)
                    );
                }
            }

            var sabrParametersCube = new Cube(optionDates, swapTenors, optionTimes, swapLengths, 8, true, backwardFlat_);
            sabrParametersCube.setLayer(0, alphas);
            sabrParametersCube.setLayer(1, betas);
            sabrParametersCube.setLayer(2, nus);
            sabrParametersCube.setLayer(3, rhos);
            sabrParametersCube.setLayer(4, forwards);
            sabrParametersCube.setLayer(5, errors);
            sabrParametersCube.setLayer(6, maxErrors);
            sabrParametersCube.setLayer(7, endCriteria);

            return sabrParametersCube;
        }

        protected void setParameterGuess()
        {
            //! set parametersGuess_ by parametersGuessQuotes_
            parametersGuess_ = new Cube(optionDates_, swapTenors_, optionTimes_, swapLengths_, 4, true, backwardFlat_);
            int i;
            for (i = 0; i < 4; i++)
            for (var j = 0; j < nOptionTenors_; j++)
            for (var k = 0; k < nSwapTenors_; k++)
            {
                parametersGuess_.setElement(i, j, k,
                    parametersGuessQuotes_[j + k * nOptionTenors_][i].link.value());
            }

            parametersGuess_.updateInterpolators();
        }

        protected SmileSection smileSection(double optionTime, double swapLength, ref Cube sabrParametersCube)
        {
            calculate();
            var sabrParameters = sabrParametersCube.value(optionTime, swapLength);
            var shiftTmp = atmVol_.link.shift(optionTime, swapLength);
            return new SabrSmileSection(optionTime, sabrParameters[4], sabrParameters, volatilityType(), shiftTmp); // ,shiftTmp
        }

        // SwaptionVolatilityCube interface
        protected override SmileSection smileSectionImpl(double optionTime, double swapLength)
        {
            if (isAtmCalibrated_)
            {
                return smileSection(optionTime, swapLength, ref denseParameters_);
            }

            return smileSection(optionTime, swapLength, ref sparseParameters_);
        }

        protected List<double> spreadVolInterpolation(Date atmOptionDate, Period atmSwapTenor)
        {
            var atmOptionTime = timeFromReference(atmOptionDate);
            var atmTimeLength = swapLength(atmSwapTenor);

            var result = new List<double>();
            var optionTimes = sparseParameters_.optionTimes();
            var swapLengths = sparseParameters_.swapLengths();
            var optionDates = sparseParameters_.optionDates();
            var swapTenors = sparseParameters_.swapTenors();

            double optionTimesPreviousNode, swapLengthsPreviousNode;

            optionTimesPreviousNode = optionTimes_.First(x => x >= System.Math.Min(atmOptionTime, optionTimes_.Max()));
            var optionTimesPreviousIndex = optionTimes_.IndexOf(optionTimesPreviousNode);

            swapLengthsPreviousNode = swapLengths_.First(x => x >= System.Math.Min(atmTimeLength, swapLengths_.Max()));
            var swapLengthsPreviousIndex = swapLengths_.IndexOf(swapLengthsPreviousNode);

            if (optionTimesPreviousIndex > 0)
            {
                optionTimesPreviousIndex--;
            }

            if (swapLengthsPreviousIndex > 0)
            {
                swapLengthsPreviousIndex--;
            }

            var smiles = new List<List<SmileSection>>();
            var smilesOnPreviousExpiry = new List<SmileSection>();
            var smilesOnNextExpiry = new List<SmileSection>();

            QLNet.Utils.QL_REQUIRE(optionTimesPreviousIndex + 1 < sparseSmiles_.Count, () =>
                "optionTimesPreviousIndex+1 >= sparseSmiles_.size()");
            QLNet.Utils.QL_REQUIRE(swapLengthsPreviousIndex + 1 < sparseSmiles_[0].Count, () =>
                "swapLengthsPreviousIndex+1 >= sparseSmiles_[0].size()");
            smilesOnPreviousExpiry.Add(sparseSmiles_[optionTimesPreviousIndex][swapLengthsPreviousIndex]);
            smilesOnPreviousExpiry.Add(sparseSmiles_[optionTimesPreviousIndex][swapLengthsPreviousIndex + 1]);
            smilesOnNextExpiry.Add(sparseSmiles_[optionTimesPreviousIndex + 1][swapLengthsPreviousIndex]);
            smilesOnNextExpiry.Add(sparseSmiles_[optionTimesPreviousIndex + 1][swapLengthsPreviousIndex + 1]);

            smiles.Add(smilesOnPreviousExpiry);
            smiles.Add(smilesOnNextExpiry);

            List<double> optionsNodes = new InitializedList<double>(2);
            optionsNodes[0] = optionTimes[optionTimesPreviousIndex];
            optionsNodes[1] = optionTimes[optionTimesPreviousIndex + 1];

            List<Date> optionsDateNodes = new InitializedList<Date>(2);
            optionsDateNodes[0] = optionDates[optionTimesPreviousIndex];
            optionsDateNodes[1] = optionDates[optionTimesPreviousIndex + 1];

            List<double> swapLengthsNodes = new InitializedList<double>(2);
            swapLengthsNodes[0] = swapLengths[swapLengthsPreviousIndex];
            swapLengthsNodes[1] = swapLengths[swapLengthsPreviousIndex + 1];

            List<Period> swapTenorNodes = new InitializedList<Period>(2);
            swapTenorNodes[0] = swapTenors[swapLengthsPreviousIndex];
            swapTenorNodes[1] = swapTenors[swapLengthsPreviousIndex + 1];

            var atmForward = atmStrike(atmOptionDate, atmSwapTenor);
            var shift = atmVol_.link.shift(atmOptionTime, atmTimeLength);

            var atmForwards = new Matrix(2, 2, 0.0);
            var atmShifts = new Matrix(2, 2, 0.0);
            var atmVols = new Matrix(2, 2, 0.0);
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    atmForwards[i, j] = atmStrike(optionsDateNodes[i], swapTenorNodes[j]);
                    atmShifts[i, j] = atmVol_.link.shift(optionsNodes[i], swapLengthsNodes[j]);
                    atmVols[i, j] = atmVol_.link.volatility(optionsDateNodes[i], swapTenorNodes[j], atmForwards[i, j]);
                    /* With the old implementation the interpolated spreads on ATM
                       volatilities were null even if the spreads on ATM volatilities to be
                       interpolated were non-zero. The new implementation removes
                       this behaviour, but introduces a small ERROR in the cube:
                       even if no spreads are applied on any cube ATM volatility corresponding
                       to quoted smile sections (that is ATM volatilities in sparse cube), the
                       cube ATM volatilities corresponding to not quoted smile sections (that
                       is ATM volatilities in dense cube) are no more exactly the quoted values,
                       but that ones PLUS the linear interpolation of the fit errors on the ATM
                       volatilities in sparse cube whose spreads are used in the calculation.
                       A similar imprecision is introduced to the volatilities in dense cube
                       whith moneyness near to 1.
                       (See below how spreadVols are calculated).
                       The extent of this error depends on the quality of the fit: in case of
                       good fits it is negligibile.
                    */
                }
            }

            for (var k = 0; k < nStrikes_; k++)
            {
                var strike = System.Math.Max(atmForward + strikeSpreads_[k], cutoffStrike_ - shift);
                var moneyness = (atmForward + shift) / (strike + shift);

                var strikes = new Matrix(2, 2, 0.0);
                var spreadVols = new Matrix(2, 2, 0.0);
                for (var i = 0; i < 2; i++)
                {
                    for (var j = 0; j < 2; j++)
                    {
                        strikes[i, j] = (atmForwards[i, j] + atmShifts[i, j]) / moneyness - atmShifts[i, j];
                        spreadVols[i, j] = smiles[i][j].volatility(strikes[i, j]) - atmVols[i, j];
                    }
                }

                var localInterpolator = new Cube(optionsDateNodes, swapTenorNodes, optionsNodes, swapLengthsNodes, 1);
                localInterpolator.setLayer(0, spreadVols);
                localInterpolator.updateInterpolators();

                result.Add(localInterpolator.value(atmOptionTime, atmTimeLength)[0]);
            }

            return result;
        }
    }
}
