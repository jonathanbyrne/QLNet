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
    //! Ornstein-Uhlenbeck process class
    /*! This class describes the Ornstein-Uhlenbeck process governed by
        \f[
            dx = a (r - x_t) dt + \sigma dW_t.
        \f]

        \ingroup processes
    */
    [PublicAPI]
    public class OrnsteinUhlenbeckProcess : StochasticProcess1D
    {
        private double level_;
        private double speed_;
        private double volatility_;
        private double x0_;

        public OrnsteinUhlenbeckProcess(double speed, double vol, double x0 = 0.0, double level = 0.0)
        {
            x0_ = x0;
            speed_ = speed;
            level_ = level;
            volatility_ = vol;
            QLNet.Utils.QL_REQUIRE(speed_ >= 0.0, () => "negative speed given");

            QLNet.Utils.QL_REQUIRE(volatility_ >= 0.0, () => "negative volatility given");
        }

        public override double diffusion(double UnnamedParameter1, double UnnamedParameter2) => volatility_;

        public override double drift(double UnnamedParameter1, double x) => speed_ * (level_ - x);

        public override double expectation(double UnnamedParameter1, double x0, double dt) => level_ + (x0 - level_) * System.Math.Exp(-speed_ * dt);

        public double level() => level_;

        public double speed() => speed_;

        public override double stdDeviation(double t, double x0, double dt) => System.Math.Sqrt(variance(t, x0, dt));

        public override double variance(double UnnamedParameter1, double UnnamedParameter2, double dt)
        {
            if (speed_ < System.Math.Sqrt(Const.QL_EPSILON))
            {
                // algebraic limit for small speed
                return volatility_ * volatility_ * dt;
            }

            return 0.5 * volatility_ * volatility_ / speed_ * (1.0 - System.Math.Exp(-2.0 * speed_ * dt));
        }

        public double volatility() => volatility_;

        // StochasticProcess interface
        public override double x0() => x0_;
    }
}
