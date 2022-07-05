/*
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
using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet.Math.statistics
{
    //! statistics class with convergence table
    /*! This class decorates another statistics class adding a
        convergence table calculation. The table tracks the
        convergence of the mean.

        It is possible to specify the number of samples at which the
        mean should be stored by mean of the second template
        parameter; the default is to store \f$ 2^{n-1} \f$ samples at
        the \f$ n \f$-th step. Any passed class must implement the
        following interface:
        \code
        Size initialSamples() const
        Size nextSamples(Size currentSamples) const
        \endcode
        as well as a copy constructor.

        \test results are tested against known good values.
    */
    [PublicAPI]
    public class ConvergenceStatistics<T> : ConvergenceStatistics<T, DoublingConvergenceSteps>
        where T : IGeneralStatistics, new()
    {
        public ConvergenceStatistics(T stats, DoublingConvergenceSteps rule) : base(stats, rule)
        {
        }

        public ConvergenceStatistics() : base(new DoublingConvergenceSteps())
        {
        }

        public ConvergenceStatistics(DoublingConvergenceSteps rule) : base(rule)
        {
        }
    }

    [PublicAPI]
    public class ConvergenceStatistics<T, U> : IGeneralStatistics
        where T : IGeneralStatistics, new()
        where U : IConvergenceSteps, new()
    {
        private int nextSampleSize_;
        private U samplingRule_;
        private List<KeyValuePair<int, double>> table_ = new List<KeyValuePair<int, double>>();

        public ConvergenceStatistics(T stats, U rule)
        {
            impl_ = stats;
            samplingRule_ = rule;

            reset();
        }

        public ConvergenceStatistics() : this(FastActivator<U>.Create())
        {
        }

        public ConvergenceStatistics(U rule)
        {
            samplingRule_ = rule;
            reset();
        }

        public void add
            (double value)
        {
            add(value, 1);
        }

        public void add
            (double value, double weight)
        {
            impl_.add(value, weight);
            if (samples() == nextSampleSize_)
            {
                table_.Add(new KeyValuePair<int, double>(samples(), mean()));
                nextSampleSize_ = samplingRule_.nextSamples(nextSampleSize_);
            }
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

        public List<KeyValuePair<int, double>> convergenceTable() => table_;

        public void reset()
        {
            impl_.reset();
            nextSampleSize_ = samplingRule_.initialSamples();
            table_.Clear();
        }

        #region wrap-up Stat

        protected T impl_ = FastActivator<T>.Create();

        public int samples() => impl_.samples();

        public double mean() => impl_.mean();

        public double min() => impl_.min();

        public double max() => impl_.max();

        public double standardDeviation() => impl_.standardDeviation();

        public double variance() => impl_.variance();

        public double skewness() => impl_.skewness();

        public double kurtosis() => impl_.kurtosis();

        public double percentile(double percent) => impl_.percentile(percent);

        public double weightSum() => impl_.weightSum();

        public double errorEstimate() => impl_.errorEstimate();

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange) =>
            impl_.expectationValue(f, inRange);

        #endregion
    }
}
