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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.MatrixUtilities;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.legacy.libormarketmodels
{
    [PublicAPI]
    public class LfmHullWhiteParameterization : LfmCovarianceParameterization
    {
        protected Matrix diffusion_, covariance_;
        protected List<double> fixingTimes_;

        public LfmHullWhiteParameterization(
            LiborForwardModelProcess process,
            OptionletVolatilityStructure capletVol,
            Matrix correlation, int factors)
            : base(process.size(), factors)
        {
            diffusion_ = new Matrix(size_ - 1, factors_);
            fixingTimes_ = process.fixingTimes();

            var sqrtCorr = new Matrix(size_ - 1, factors_, 1.0);
            if (correlation.empty())
            {
                QLNet.Utils.QL_REQUIRE(factors_ == 1, () => "correlation matrix must be given for multi factor models");
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(correlation.rows() == size_ - 1 &&
                                                correlation.rows() == correlation.columns(), () => "wrong dimesion of the correlation matrix");

                QLNet.Utils.QL_REQUIRE(factors_ <= size_ - 1, () => "too many factors for given LFM process");

                var tmpSqrtCorr = MatrixUtilitites.pseudoSqrt(correlation,
                    MatrixUtilitites.SalvagingAlgorithm.Spectral);

                // reduce to n factor model
                // "Reconstructing a valid correlation matrix from invalid data"
                // (<http://www.quarchome.org/correlationmatrix.pdf>)
                for (var i = 0; i < size_ - 1; ++i)
                {
                    double d = 0;
                    tmpSqrtCorr.row(i).GetRange(0, factors_).ForEach((ii, vv) => d += vv * tmpSqrtCorr.row(i)[ii]);
                    for (var k = 0; k < factors_; ++k)
                    {
                        sqrtCorr[i, k] = tmpSqrtCorr.row(i).GetRange(0, factors_)[k] / System.Math.Sqrt(d);
                    }
                }
            }

            var lambda = new List<double>();
            var dayCounter = process.index().dayCounter();
            var fixingTimes = process.fixingTimes();
            var fixingDates = process.fixingDates();

            for (var i = 1; i < size_; ++i)
            {
                var cumVar = 0.0;
                for (var j = 1; j < i; ++j)
                {
                    cumVar += lambda[i - j - 1] * lambda[i - j - 1]
                                                * (fixingTimes[j + 1] - fixingTimes[j]);
                }

                var vol = capletVol.volatility(fixingDates[i], 0.0);
                var var = vol * vol
                              * capletVol.dayCounter().yearFraction(fixingDates[0],
                                  fixingDates[i]);
                lambda.Add(System.Math.Sqrt((var - cumVar)
                                            / (fixingTimes[1] - fixingTimes[0])));
                for (var q = 0; q < factors_; ++q)
                {
                    diffusion_[i - 1, q] = sqrtCorr[i - 1, q] * lambda.Last();
                }
            }

            covariance_ = diffusion_ * Matrix.transpose(diffusion_);
        }

        public LfmHullWhiteParameterization(
            LiborForwardModelProcess process,
            OptionletVolatilityStructure capletVol)
            : this(process, capletVol, new Matrix(), 1)
        {
        }

        public override Matrix covariance(double t) => covariance(t, null);

        public override Matrix covariance(double t, Vector x)
        {
            var tmp = new Matrix(size_, size_, 0.0);
            var m = nextIndexReset(t);

            for (var k = m; k < size_; ++k)
            {
                for (var i = m; i < size_; ++i)
                {
                    tmp[k, i] = covariance_[k - m, i - m];
                }
            }

            return tmp;
        }

        public override Matrix diffusion(double t) => diffusion(t, null);

        public override Matrix diffusion(double t, Vector x)
        {
            var tmp = new Matrix(size_, factors_, 0.0);
            var m = nextIndexReset(t);

            for (var k = m; k < size_; ++k)
            {
                for (var q = 0; q < factors_; ++q)
                {
                    tmp[k, q] = diffusion_[k - m, q];
                }
            }

            return tmp;
        }

        public override Matrix integratedCovariance(double t, Vector x = null)
        {
            var tmp = new Matrix(size_, size_, 0.0);
            var last = fixingTimes_.BinarySearch(t);
            if (last < 0)
                //Lower_bound is a version of binary search: it attempts to find the element value in an ordered range [first, last)
                // [1]. Specifically, it returns the first position where value could be inserted without violating the ordering.
                // [2] The first version of lower_bound uses operator< for comparison, and the second uses the function object comp.
                // lower_bound returns the furthermost iterator i in [first, last) such that, for every iterator j in [first, i), *j < value.
            {
                last = ~last;
            }

            for (var i = 0; i <= last; ++i)
            {
                var dt = (i < last ? fixingTimes_[i + 1] : t)
                         - fixingTimes_[i];

                for (var k = i; k < size_ - 1; ++k)
                {
                    for (var l = i; l < size_ - 1; ++l)
                    {
                        tmp[k + 1, l + 1] += covariance_[k - i, l - i] * dt;
                    }
                }
            }

            return tmp;
        }

        protected int nextIndexReset(double t)
        {
            var result = fixingTimes_.BinarySearch(t);
            if (result < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
            {
                result = ~result - 1;
            }

            // impose limits. we need the one before last at max or the first at min
            result = System.Math.Max(System.Math.Min(result, fixingTimes_.Count - 2), 0);
            return result + 1;
        }
    }
}
