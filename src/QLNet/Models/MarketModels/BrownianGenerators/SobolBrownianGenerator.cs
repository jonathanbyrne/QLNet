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
using QLNet.Math.Distributions;
using QLNet.Math.randomnumbers;
using QLNet.Methods.montecarlo;

namespace QLNet.Models.MarketModels.BrownianGenerators
{
    //! Sobol Brownian generator for market-model simulations
    /*! Incremental Brownian generator using a Sobol generator,
        inverse-cumulative Gaussian method, and Brownian bridging.
    */
    [PublicAPI]
    public class SobolBrownianGenerator : IBrownianGenerator
    {
        public enum Ordering
        {
            Factors, /*!< The variates with the best quality will be
                        used for the evolution of the first factor. */
            Steps, /*!< The variates with the best quality will be
                        used for the largest steps of all factors. */
            Diagonal /*!< A diagonal schema will be used to assign
                        the variates with the best quality to the
                        most important factors and the largest
                        steps. */
        }

        private BrownianBridge bridge_;
        private List<List<double>> bridgedVariates_;
        private int factors_, steps_;
        private InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal> generator_;
        // work variables
        private int lastStep_;
        private List<List<int>> orderedIndices_;
        private Ordering ordering_;

        public SobolBrownianGenerator(int factors, int steps, Ordering ordering, ulong seed = 0,
            SobolRsg.DirectionIntegers directionIntegers = SobolRsg.DirectionIntegers.Jaeckel)
        {
            factors_ = factors;
            steps_ = steps;
            ordering_ = ordering;
            generator_ = new InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal>(
                new SobolRsg(factors * steps, seed, directionIntegers), new InverseCumulativeNormal());
            bridge_ = new BrownianBridge(steps);
            lastStep_ = 0;
            orderedIndices_ = new InitializedList<List<int>>(factors);
            bridgedVariates_ = new InitializedList<List<double>>(factors);
            for (var i = 0; i < factors; i++)
            {
                orderedIndices_[i] = new InitializedList<int>(steps);
                bridgedVariates_[i] = new InitializedList<double>(steps);
            }

            switch (ordering_)
            {
                case Ordering.Factors:
                    fillByFactor(orderedIndices_, factors_, steps_);
                    break;
                case Ordering.Steps:
                    fillByStep(orderedIndices_, factors_, steps_);
                    break;
                case Ordering.Diagonal:
                    fillByDiagonal(orderedIndices_, factors_, steps_);
                    break;
                default:
                    Utils.QL_FAIL("unknown ordering");
                    break;
            }
        }

        public double nextPath()
        {
            var sample = generator_.nextSequence();
            // Brownian-bridge the variates according to the ordered indices
            for (var i = 0; i < factors_; ++i)
            {
                var permList = new List<double>();
                foreach (var index in orderedIndices_[i])
                {
                    permList.Add(sample.value[index]);
                }

                bridge_.transform(permList, bridgedVariates_[i]); // TODO Check
            }

            lastStep_ = 0;
            return sample.weight;
        }

        public double nextStep(List<double> output)
        {
#if QL_EXTRA_SAFETY_CHECKS
         Utils.QL_REQUIRE(output.Count == factors_, () => "size mismatch");
         Utils.QL_REQUIRE(lastStep_<steps_, () => "sequence exhausted");
#endif
            for (var i = 0; i < factors_; ++i)
            {
                output[i] = bridgedVariates_[i][lastStep_];
            }

            ++lastStep_;
            return 1.0;
        }

        public int numberOfFactors() => factors_;

        public int numberOfSteps() => steps_;

        // test interface
        public List<List<int>> orderedIndices() => orderedIndices_;

        public List<List<double>> transform(List<List<double>> variates)
        {
            Utils.QL_REQUIRE(variates.Count == factors_ * steps_, () => "inconsistent variate vector");

            var dim = factors_ * steps_;
            var nPaths = variates.First().Count;

            List<List<double>> retVal = new InitializedList<List<double>>(factors_, new InitializedList<double>(nPaths * steps_));

            for (var j = 0; j < nPaths; ++j)
            {
                List<double> sample = new InitializedList<double>(steps_ * factors_);
                for (var k = 0; k < dim; ++k)
                {
                    sample[k] = variates[k][j];
                }

                for (var i = 0; i < factors_; ++i)
                {
                    var permList = new List<double>();
                    foreach (var index in orderedIndices_[i])
                    {
                        permList.Add(sample[index]);
                    }

                    var temp = retVal[i].GetRange(j * steps_, retVal[i].Count - j * steps_);
                    bridge_.transform(permList, temp); // TODO Check
                }
            }

            return retVal;
        }

        // variate 2 is used for the second factor's full path
        private void fillByDiagonal(List<List<int>> M, int factors, int steps)
        {
            // starting position of the current diagonal
            int i0 = 0, j0 = 0;
            // current position
            int i = 0, j = 0;
            var counter = 0;
            while (counter < factors * steps)
            {
                M[i][j] = counter++;
                if (i == 0 || j == steps - 1)
                {
                    // we completed a diagonal and have to start a new one
                    if (i0 < factors - 1)
                    {
                        // we start the path of the next factor
                        i0 = i0 + 1;
                        j0 = 0;
                    }
                    else
                    {
                        // we move along the path of the last factor
                        i0 = factors - 1;
                        j0 = j0 + 1;
                    }

                    i = i0;
                    j = j0;
                }
                else
                {
                    // we move along the diagonal
                    i = i - 1;
                    j = j + 1;
                }
            }
        }

        private void fillByFactor(List<List<int>> M, int factors, int steps)
        {
            var counter = 0;
            for (var i = 0; i < factors; ++i)
            for (var j = 0; j < steps; ++j)
            {
                M[i][j] = counter++;
            }
        }

        private void fillByStep(List<List<int>> M, int factors, int steps)
        {
            var counter = 0;
            for (var j = 0; j < steps; ++j)
            for (var i = 0; i < factors; ++i)
            {
                M[i][j] = counter++;
            }
        }
    }
}
