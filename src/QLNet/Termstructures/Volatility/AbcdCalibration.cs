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

using QLNet.Math;
using QLNet.Math.Optimization;
using System;
using System.Collections.Generic;
using QLNet.Math.Distributions;

namespace QLNet.Termstructures.Volatility
{
    [JetBrains.Annotations.PublicAPI] public class AbcdCalibration
    {
        private class AbcdError : CostFunction
        {
            public AbcdError(AbcdCalibration abcd)
            {
                abcd_ = abcd;
            }

            public override double value(Vector x)
            {
                var y = abcd_.transformation_.direct(x);
                abcd_.a_ = y[0];
                abcd_.b_ = y[1];
                abcd_.c_ = y[2];
                abcd_.d_ = y[3];
                return abcd_.error();
            }

            public override Vector values(Vector x)
            {
                var y = abcd_.transformation_.direct(x);
                abcd_.a_ = y[0];
                abcd_.b_ = y[1];
                abcd_.c_ = y[2];
                abcd_.d_ = y[3];
                return abcd_.errors();
            }

            private AbcdCalibration abcd_;
        }

        private class AbcdParametersTransformation : IParametersTransformation
        {
            public AbcdParametersTransformation()
            {
                y_ = new Vector(4);
            }
            // to constrained <- from unconstrained
            public Vector direct(Vector x)
            {
                y_[1] = x[1];
                y_[2] = System.Math.Exp(x[2]);
                y_[3] = System.Math.Exp(x[3]);
                y_[0] = System.Math.Exp(x[0]) - y_[3];
                return y_;
            }

            // to unconstrained <- from constrained
            public Vector inverse(Vector x)
            {
                y_[1] = x[1];
                y_[2] = System.Math.Log(x[2]);
                y_[3] = System.Math.Log(x[3]);
                y_[0] = System.Math.Log(x[0] + x[3]);
                return y_;
            }

            private Vector y_;
        }


        public AbcdCalibration() { }

        // to constrained <- from unconstrained
        public AbcdCalibration(List<double> t,
                               List<double> blackVols,
                               double aGuess = -0.06,
                               double bGuess = 0.17,
                               double cGuess = 0.54,
                               double dGuess = 0.17,
                               bool aIsFixed = false,
                               bool bIsFixed = false,
                               bool cIsFixed = false,
                               bool dIsFixed = false,
                               bool vegaWeighted = false,
                               EndCriteria endCriteria = null,
                               OptimizationMethod method = null)
        {
            aIsFixed_ = aIsFixed;
            bIsFixed_ = bIsFixed;
            cIsFixed_ = cIsFixed;
            dIsFixed_ = dIsFixed;
            a_ = aGuess;
            b_ = bGuess;
            c_ = cGuess;
            d_ = dGuess;
            abcdEndCriteria_ = EndCriteria.Type.None;
            endCriteria_ = endCriteria;
            optMethod_ = method;
            weights_ = new InitializedList<double>(blackVols.Count, 1.0 / blackVols.Count);
            vegaWeighted_ = vegaWeighted;
            times_ = t;
            blackVols_ = blackVols;


            AbcdMathFunction.validate(aGuess, bGuess, cGuess, dGuess);

            Utils.QL_REQUIRE(blackVols.Count == t.Count, () =>
                             "mismatch between number of times (" + t.Count + ") and blackVols (" + blackVols.Count + ")");

            // if no optimization method or endCriteria is provided, we provide one
            if (optMethod_ == null)
            {
                var epsfcn = 1.0e-8;
                var xtol = 1.0e-8;
                var gtol = 1.0e-8;
                var useCostFunctionsJacobian = false;
                optMethod_ = new LevenbergMarquardt(epsfcn, xtol, gtol, useCostFunctionsJacobian);
            }

            if (endCriteria_ == null)
            {
                var maxIterations = 10000;
                var maxStationaryStateIterations = 1000;
                var rootEpsilon = 1.0e-8;
                var functionEpsilon = 0.3e-4;     // Why 0.3e-4 ?
                var gradientNormEpsilon = 0.3e-4; // Why 0.3e-4 ?
                endCriteria_ = new EndCriteria(maxIterations, maxStationaryStateIterations, rootEpsilon, functionEpsilon,
                                               gradientNormEpsilon);
            }
        }

        //! adjustment factors needed to match Black vols
        public List<double> k(List<double> t, List<double> blackVols)
        {
            Utils.QL_REQUIRE(blackVols.Count == t.Count, () =>
                             "mismatch between number of times (" + t.Count + ") and blackVols (" + blackVols.Count + ")");
            List<double> k = new InitializedList<double>(t.Count);
            for (var i = 0; i < t.Count; i++)
            {
                k[i] = blackVols[i] / value(t[i]);
            }
            return k;
        }


        public void compute()
        {
            if (vegaWeighted_)
            {
                var weightsSum = 0.0;
                for (var i = 0; i < times_.Count; i++)
                {
                    var stdDev = System.Math.Sqrt(blackVols_[i] * blackVols_[i] * times_[i]);
                    // when strike==forward, the blackFormulaStdDevDerivative becomes
                    weights_[i] = new CumulativeNormalDistribution().derivative(.5 * stdDev);
                    weightsSum += weights_[i];
                }
                // weight normalization
                for (var i = 0; i < times_.Count; i++)
                {
                    weights_[i] /= weightsSum;
                }

            }
            // there is nothing to optimize
            if (aIsFixed_ && bIsFixed_ && cIsFixed_ && dIsFixed_)
            {
                abcdEndCriteria_ = EndCriteria.Type.None;
                return;
            }
            else
            {
                var costFunction = new AbcdError(this);
                transformation_ = new AbcdParametersTransformation();

                var guess = new Vector(4);
                guess[0] = a_;
                guess[1] = b_;
                guess[2] = c_;
                guess[3] = d_;

                List<bool> parameterAreFixed = new InitializedList<bool>(4);
                parameterAreFixed[0] = aIsFixed_;
                parameterAreFixed[1] = bIsFixed_;
                parameterAreFixed[2] = cIsFixed_;
                parameterAreFixed[3] = dIsFixed_;

                var inversedTransformatedGuess = new Vector(transformation_.inverse(guess));

                var projectedAbcdCostFunction = new ProjectedCostFunction(costFunction,
                                                                                            inversedTransformatedGuess, parameterAreFixed);

                var projectedGuess = new Vector(projectedAbcdCostFunction.project(inversedTransformatedGuess));

                var constraint = new NoConstraint();
                var problem = new Problem(projectedAbcdCostFunction, constraint, projectedGuess);
                abcdEndCriteria_ = optMethod_.minimize(problem, endCriteria_);
                var projectedResult = new Vector(problem.currentValue());
                var transfResult = new Vector(projectedAbcdCostFunction.include(projectedResult));

                var result = transformation_.direct(transfResult);
                AbcdMathFunction.validate(a_, b_, c_, d_);
                a_ = result[0];
                b_ = result[1];
                c_ = result[2];
                d_ = result[3];
            }
        }

        //calibration results
        public double value(double x) => abcdBlackVolatility(x, a_, b_, c_, d_);

        public double error()
        {
            var n = times_.Count;
            double error, squaredError = 0.0;
            for (var i = 0; i < times_.Count; i++)
            {
                error = value(times_[i]) - blackVols_[i];
                squaredError += error * error * weights_[i];
            }
            return System.Math.Sqrt(n * squaredError / (n - 1));
        }

        public double maxError()
        {
            double error, maxError = double.MinValue;
            for (var i = 0; i < times_.Count; i++)
            {
                error = System.Math.Abs(value(times_[i]) - blackVols_[i]);
                maxError = System.Math.Max(maxError, error);
            }
            return maxError;
        }

        public Vector errors()
        {
            var results = new Vector(times_.Count);
            for (var i = 0; i < times_.Count; i++)
            {
                results[i] = (value(times_[i]) - blackVols_[i]) * System.Math.Sqrt(weights_[i]);
            }
            return results;
        }

        public double abcdBlackVolatility(double u, double a, double b, double c, double d)
        {
            var model = new AbcdFunction(a, b, c, d);
            return model.volatility(0.0, u, u);
        }

        public EndCriteria.Type endCriteria() => abcdEndCriteria_;

        public double a() => a_;

        public double b() => b_;

        public double c() => c_;

        public double d() => d_;

        public bool aIsFixed_ { get; set; }
        public bool bIsFixed_ { get; set; }
        public bool cIsFixed_ { get; set; }
        public bool dIsFixed_ { get; set; }
        private double a_, b_, c_, d_;
        public IParametersTransformation transformation_ { get; set; }


        // optimization method used for fitting
        private EndCriteria.Type abcdEndCriteria_;
        private EndCriteria endCriteria_;
        private OptimizationMethod optMethod_;
        private List<double> weights_;
        private bool vegaWeighted_;
        //! Parameters
        private List<double> times_, blackVols_;

    }
}
