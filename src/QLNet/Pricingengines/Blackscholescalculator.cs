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
using QLNet.Instruments;

namespace QLNet.PricingEngines
{
    //! Black-Scholes 1973 calculator class
    [PublicAPI]
    public class BlackScholesCalculator : BlackCalculator
    {
        protected double growth_;
        protected double spot_;

        public BlackScholesCalculator(StrikedTypePayoff payoff, double spot, double growth, double stdDev, double discount)
            : base(payoff, spot * growth / discount, stdDev, discount)
        {
            spot_ = spot;
            growth_ = growth;

            QLNet.Utils.QL_REQUIRE(spot_ >= 0.0, () => "positive spot value required: " + spot_ + " not allowed");
            QLNet.Utils.QL_REQUIRE(growth_ >= 0.0, () => "positive growth value required: " + growth_ + " not allowed");
        }

        //! Sensitivity to change in the underlying spot price.

        public double delta() => base.delta(spot_);

        //        ! Sensitivity in percent to a percent change in the
        //            underlying spot price.
        public double elasticity() => base.elasticity(spot_);

        //        ! Second order derivative with respect to change in the
        //            underlying spot price.
        public double gamma() => base.gamma(spot_);

        //! Sensitivity to time to maturity.
        public double theta(double maturity) => base.theta(spot_, maturity);

        //        ! Sensitivity to time to maturity per day
        //            (assuming 365 day in a year).
        public double thetaPerDay(double maturity) => base.thetaPerDay(spot_, maturity);
    }
}
