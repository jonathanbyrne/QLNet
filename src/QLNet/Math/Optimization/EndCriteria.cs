/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 *
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

namespace QLNet.Math.Optimization
{
    //! Criteria to end optimization process:
    //    ! - maximum number of iterations AND minimum number of iterations around stationary point
    //        - x (independent variable) stationary point
    //        - y=f(x) (dependent variable) stationary point
    //        - stationary gradient
    //
    [PublicAPI]
    public class EndCriteria
    {
        public enum Type
        {
            None,
            MaxIterations,
            StationaryPoint,
            StationaryFunctionValue,
            StationaryFunctionAccuracy,
            ZeroGradientNorm,
            Unknown
        }

        protected double functionEpsilon_;
        protected double? gradientNormEpsilon_;

        //! Maximum number of iterations
        protected int maxIterations_;
        //! Maximun number of iterations in stationary state
        protected int? maxStationaryStateIterations_;
        //! root, function and gradient epsilons
        protected double rootEpsilon_;

        //! Initialization constructor
        public EndCriteria(int maxIterations, int? maxStationaryStateIterations, double rootEpsilon, double functionEpsilon, double? gradientNormEpsilon)
        {
            maxIterations_ = maxIterations;
            maxStationaryStateIterations_ = maxStationaryStateIterations;
            rootEpsilon_ = rootEpsilon;
            functionEpsilon_ = functionEpsilon;
            gradientNormEpsilon_ = gradientNormEpsilon;

            if (maxStationaryStateIterations_ == null)
            {
                maxStationaryStateIterations_ = System.Math.Min(maxIterations / 2, 100);
            }

            Utils.QL_REQUIRE(maxStationaryStateIterations_ > 1, () =>
                "maxStationaryStateIterations_ (" + maxStationaryStateIterations_ + ") must be greater than one");

            Utils.QL_REQUIRE(maxStationaryStateIterations_ < maxIterations_, () =>
                "maxStationaryStateIterations_ (" + maxStationaryStateIterations_ + ") must be less than maxIterations_ (" + maxIterations_ + ")");

            if (gradientNormEpsilon_ == null)
            {
                gradientNormEpsilon_ = functionEpsilon_;
            }
        }

        //! Test if the number of iteration is below MaxIterations
        public bool checkMaxIterations(int iteration, ref Type ecType)
        {
            if (iteration < maxIterations_)
            {
                return false;
            }

            ecType = Type.MaxIterations;
            return true;
        }

        //! Test if the function value is below functionEpsilon
        public bool checkStationaryFunctionAccuracy(double f, bool positiveOptimization, ref Type ecType)
        {
            if (!positiveOptimization)
            {
                return false;
            }

            if (f >= functionEpsilon_)
            {
                return false;
            }

            ecType = Type.StationaryFunctionAccuracy;
            return true;
        }

        //! Test if the function variation is below functionEpsilon
        public bool checkStationaryFunctionValue(double fxOld, double fxNew, ref int statStateIterations, ref Type ecType)
        {
            if (System.Math.Abs(fxNew - fxOld) >= functionEpsilon_)
            {
                statStateIterations = 0;
                return false;
            }

            ++statStateIterations;
            if (statStateIterations <= maxStationaryStateIterations_)
            {
                return false;
            }

            ecType = Type.StationaryFunctionValue;
            return true;
        }

        //! Test if the root variation is below rootEpsilon
        public bool checkStationaryPoint(double xOld, double xNew, ref int statStateIterations, ref Type ecType)
        {
            if (System.Math.Abs(xNew - xOld) >= rootEpsilon_)
            {
                statStateIterations = 0;
                return false;
            }

            ++statStateIterations;
            if (statStateIterations <= maxStationaryStateIterations_)
            {
                return false;
            }

            ecType = Type.StationaryPoint;
            return true;
        }

        public bool checkZeroGradientNorm(double gradientNorm, ref Type ecType)
        {
            if (gradientNorm >= gradientNormEpsilon_)
            {
                return false;
            }

            ecType = Type.ZeroGradientNorm;
            return true;
        }

        public double functionEpsilon() => functionEpsilon_;

        public double gradientNormEpsilon() => gradientNormEpsilon_.GetValueOrDefault();

        // Inspectors

        // Inspectors
        public int maxIterations() => maxIterations_;

        public int maxStationaryStateIterations() => maxStationaryStateIterations_.GetValueOrDefault();

        public double rootEpsilon() => rootEpsilon_;

        //        ! Test if the number of iterations is not too big
        //            and if a minimum point is not reached
        public bool value(int iteration, ref int statStateIterations, bool positiveOptimization, double fold, double UnnamedParameter1, double fnew, double normgnew, ref Type ecType) => checkMaxIterations(iteration, ref ecType) || checkStationaryFunctionValue(fold, fnew, ref statStateIterations, ref ecType) || checkStationaryFunctionAccuracy(fnew, positiveOptimization, ref ecType) || checkZeroGradientNorm(normgnew, ref ecType);
    }
}
