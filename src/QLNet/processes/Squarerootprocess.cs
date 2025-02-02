/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

namespace QLNet.Processes
{
    //! Square-root process class
    //    ! This class describes a square-root process governed by
    //        \f[
    //            dx = a (b - x_t) dt + \sigma \sqrt{x_t} dW_t.
    //        \f]
    //
    //        \ingroup processes
    //
    [PublicAPI]
    public class SquareRootProcess : StochasticProcess1D
    {
        private double mean_;
        private double speed_;
        private double volatility_;
        private double x0_;

        public SquareRootProcess(double b, double a, double sigma, double x0) : this(b, a, sigma, x0, new EulerDiscretization())
        {
        }

        public SquareRootProcess(double b, double a, double sigma) : this(b, a, sigma, 0.0, new EulerDiscretization())
        {
        }

        public SquareRootProcess(double b, double a, double sigma, double x0, IDiscretization1D disc)
            : base(disc)
        {
            x0_ = x0;
            mean_ = b;
            speed_ = a;
            volatility_ = sigma;
        }

        public override double diffusion(double UnnamedParameter1, double x) => volatility_ * System.Math.Sqrt(x);

        public override double drift(double UnnamedParameter1, double x) => speed_ * (mean_ - x);

        // StochasticProcess interface
        public override double x0() => x0_;
    }
}
