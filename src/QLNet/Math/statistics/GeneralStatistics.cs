﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using JetBrains.Annotations;

namespace QLNet.Math.statistics
{
    //! Statistics tool
    /*! This class accumulates a set of data and returns their
        statistics (e.g: mean, variance, skewness, kurtosis,
        error estimation, percentile, etc.) based on the empirical
        distribution (no gaussian assumption)

        It doesn't suffer the numerical instability problem of
        IncrementalStatistics. The downside is that it stores all
        samples, thus increasing the memory requirements.
    */
    [PublicAPI]
    public class GeneralStatistics : IGeneralStatistics
    {
        private double? mean_, weightSum_, variance_, skewness_, kurtosis_;
        private List<KeyValuePair<double, double>> samples_;
        private bool sorted_;

        public GeneralStatistics()
        {
            reset();
        }

        //! adds a datum to the set, possibly with a weight
        public void add
            (double value)
        {
            add(value, 1);
        }

        public void add
            (double value, double weight)
        {
            QLNet.Utils.QL_REQUIRE(weight >= 0.0, () => "negative weight not allowed");
            samples_.Add(new KeyValuePair<double, double>(value, weight));

            sorted_ = false;
            mean_ = weightSum_ = variance_ = skewness_ = kurtosis_ = null;
        }

        //! adds a sequence of data to the set, with default weight
        public void addSequence(List<double> list)
        {
            foreach (var v in list)
            {
                add
                    (v, 1);
            }
        }

        //! adds a sequence of data to the set, each with its weight
        public void addSequence(List<double> data, List<double> weight)
        {
            for (var i = 0; i < data.Count; i++)
            {
                add
                    (data[i], weight[i]);
            }
        }

        //! collected data
        public List<KeyValuePair<double, double>> data() => samples_;

        /*! returns the error estimate on the mean value, defined as
            \f$ \epsilon = \sigma/\sqrt{N}. \f$ */
        public double errorEstimate() => System.Math.Sqrt(variance() / samples());

        /*! Expectation value of a function \f$ f \f$ on a given range \f$ \mathcal{R} \f$, i.e.,

            The range is passed as a boolean function returning
            <tt>true</tt> if the argument belongs to the range
            or <tt>false</tt> otherwise.

            The function returns a pair made of the result and the number of observations in the given range. */
        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange)
        {
            double num = 0.0, den = 0.0;
            var N = 0;

            foreach (var x in samples_.Where(x => inRange(x)))
            {
                num += f(x) * x.Value;
                den += x.Value;
                N += 1;
            }

            if (N == 0)
            {
                return new KeyValuePair<double, int>(0, 0);
            }

            return new KeyValuePair<double, int>(num / den, N);
        }

        /*! returns the excess kurtosis
            The above evaluates to 0 for a Gaussian distribution.
        */
        public double kurtosis()
        {
            if (kurtosis_ == null)
            {
                var N = samples();
                QLNet.Utils.QL_REQUIRE(N > 3, () => "sample number <=3, unsufficient");

                var x = expectationValue(y => System.Math.Pow(y.Key * y.Value - mean(), 4), y => true).Key;
                var sigma2 = variance();

                var c1 = N / (N - 1.0) * (N / (N - 2.0)) * ((N + 1.0) / (N - 3.0));
                var c2 = 3.0 * ((N - 1.0) / (N - 2.0)) * ((N - 1.0) / (N - 3.0));

                kurtosis_ = c1 * (x / (sigma2 * sigma2)) - c2;
            }

            return kurtosis_.GetValueOrDefault();
        }

        /*! returns the maximum sample value */
        public double max()
        {
            QLNet.Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
            return samples_.Max(x => x.Key);
        }

        /*! returns the mean, defined as
            \f[ \langle x \rangle = \frac{\sum w_i x_i}{\sum w_i}. \f] */
        public double mean()
        {
            if (mean_ == null)
            {
                var N = samples();
                QLNet.Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
                // eat our own dog food
                mean_ = expectationValue(x => x.Key * x.Value, x => true).Key;
            }

            return mean_.GetValueOrDefault();
        }

        /*! returns the minimum sample value */
        public double min()
        {
            QLNet.Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
            return samples_.Min(x => x.Key);
        }

        /*! \f$ y \f$-th percentile, defined as the value \f$ \bar{x} \f$
            \pre \f$ y \f$ must be in the range \f$ (0-1]. \f$
        */
        public double percentile(double percent)
        {
            QLNet.Utils.QL_REQUIRE(percent > 0.0 && percent <= 1.0, () => "percentile (" + percent + ") must be in (0.0, 1.0]");

            var sampleWeight = weightSum();
            QLNet.Utils.QL_REQUIRE(sampleWeight > 0, () => "empty sample set");

            sort();

            double integral = 0, target = percent * sampleWeight;
            var pos = samples_.Count(x =>
            {
                integral += x.Value;
                return integral < target;
            });
            return samples_[pos].Key;
        }

        //! resets the data to a null set
        public void reset()
        {
            samples_ = new List<KeyValuePair<double, double>>();

            sorted_ = true;
            mean_ = weightSum_ = variance_ = skewness_ = kurtosis_ = null;
        }

        //! number of samples collected
        public int samples() => samples_.Count;

        /*! returns the skewness, defined as
            \f[ \frac{N^2}{(N-1)(N-2)} \frac{\left\langle \left(
                x-\langle x \rangle \right)^3 \right\rangle}{\sigma^3}. \f]
            The above evaluates to 0 for a Gaussian distribution.
        */
        public double skewness()
        {
            if (skewness_ == null)
            {
                var N = samples();
                QLNet.Utils.QL_REQUIRE(N > 2, () => "sample number <=2, unsufficient");

                var x = expectationValue(y => System.Math.Pow(y.Key * y.Value - mean(), 3), y => true).Key;
                var sigma = standardDeviation();

                skewness_ = x / System.Math.Pow(sigma, 3) * (N / (N - 1.0)) * (N / (N - 2.0));
            }

            return skewness_.GetValueOrDefault();
        }

        //! sort the data set in increasing order
        public void sort()
        {
            if (!sorted_)
            {
                samples_.Sort((x, y) => x.Key.CompareTo(y.Key));
                sorted_ = true;
            }
        }

        /*! returns the standard deviation \f$ \sigma \f$, defined as the
        square root of the variance. */
        public double standardDeviation() => System.Math.Sqrt(variance());

        /*! \f$ y \f$-th top percentile, defined as the value
            \pre \f$ y \f$ must be in the range \f$ (0-1]. \f$
        */
        public double topPercentile(double percent)
        {
            QLNet.Utils.QL_REQUIRE(percent > 0.0 && percent <= 1.0, () => "percentile (" + percent + ") must be in (0.0, 1.0]");

            var sampleWeight = weightSum();
            QLNet.Utils.QL_REQUIRE(sampleWeight > 0, () => "empty sample set");

            sort();

            double integral = 0, target = 1 - percent * sampleWeight;
            var pos = samples_.Count(x =>
            {
                integral += x.Value;
                return integral < target;
            });
            return samples_[pos].Key;
        }

        /*! returns the variance, defined as
            \f[ \sigma^2 = \frac{N}{N-1} \left\langle \left(
                x-\langle x \rangle \right)^2 \right\rangle. \f] */
        public double variance()
        {
            if (variance_ == null)
            {
                var N = samples();
                QLNet.Utils.QL_REQUIRE(N > 1, () => "sample number <=1, unsufficient");
                // Subtract the mean and square. Repeat on the whole range.
                // Hopefully, the whole thing will be inlined in a single loop.
                var s2 = expectationValue(x => System.Math.Pow(x.Key * x.Value - mean(), 2), x => true).Key;
                variance_ = s2 * N / (N - 1.0);
            }

            return variance_.GetValueOrDefault();
        }

        //! sum of data weights
        public double weightSum()
        {
            if (weightSum_ == null)
            {
                weightSum_ = samples_.Sum(x => x.Value);
            }

            return weightSum_.GetValueOrDefault();
        }
    }
}
