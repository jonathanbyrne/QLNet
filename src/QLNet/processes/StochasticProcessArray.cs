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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.matrixutilities;
using QLNet.Time;

namespace QLNet.processes
{
    //! %Array of correlated 1-D stochastic processes
    /*! \ingroup processes */
    [PublicAPI]
    public class StochasticProcessArray : StochasticProcess
    {
        protected List<StochasticProcess1D> processes_;
        protected Matrix sqrtCorrelation_;

        public StochasticProcessArray(List<StochasticProcess1D> processes, Matrix correlation)
        {
            processes_ = processes;
            sqrtCorrelation_ = MatrixUtilitites.pseudoSqrt(correlation, MatrixUtilitites.SalvagingAlgorithm.Spectral);

            Utils.QL_REQUIRE(processes.Count != 0, () => "no processes given");
            Utils.QL_REQUIRE(correlation.rows() == processes.Count, () =>
                "mismatch between number of processes and size of correlation matrix");
            for (var i = 0; i < processes_.Count; i++)
            {
                processes_[i].registerWith(update);
            }
        }

        public override Vector apply(Vector x0, Vector dx)
        {
            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = processes_[i].apply(x0[i], dx[i]);
            }

            return tmp;
        }

        public Matrix correlation() => sqrtCorrelation_ * Matrix.transpose(sqrtCorrelation_);

        public override Matrix covariance(double t0, Vector x0, double dt)
        {
            var tmp = stdDeviation(t0, x0, dt);
            return tmp * Matrix.transpose(tmp);
        }

        public override Matrix diffusion(double t, Vector x)
        {
            var tmp = sqrtCorrelation_;
            for (var i = 0; i < size(); ++i)
            {
                var sigma = processes_[i].diffusion(t, x[i]);
                for (var j = 0; j < tmp.columns(); j++)
                {
                    tmp[i, j] *= sigma;
                }
            }

            return tmp;
        }

        public override Vector drift(double t, Vector x)
        {
            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = processes_[i].drift(t, x[i]);
            }

            return tmp;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw)
        {
            var dz = sqrtCorrelation_ * dw;

            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = processes_[i].evolve(t0, x0[i], dt, dz[i]);
            }

            return tmp;
        }

        public override Vector expectation(double t0, Vector x0, double dt)
        {
            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = processes_[i].expectation(t0, x0[i], dt);
            }

            return tmp;
        }

        public override Vector initialValues()
        {
            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = processes_[i].x0();
            }

            return tmp;
        }

        // inspectors
        public StochasticProcess1D process(int i) => processes_[i];

        // stochastic process interface
        public override int size() => processes_.Count;

        public override Matrix stdDeviation(double t0, Vector x0, double dt)
        {
            var tmp = sqrtCorrelation_;
            for (var i = 0; i < size(); ++i)
            {
                var sigma = processes_[i].stdDeviation(t0, x0[i], dt);
                for (var j = 0; j < tmp.columns(); j++)
                {
                    tmp[i, j] *= sigma;
                }
            }

            return tmp;
        }

        public override double time(Date d) => processes_[0].time(d);
    }
}
