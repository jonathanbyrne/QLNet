//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//                2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility
{
    [PublicAPI]
    public class SviSmileSection : SmileSection
    {
        public SviSmileSection(double timeToExpiry, double forward,
            List<double> sviParameters)
            : base(timeToExpiry)
        {
            forward_ = forward;
            param_ = sviParameters;
            init();
        }

        public SviSmileSection(Date d, double forward,
            List<double> sviParameters,
            DayCounter dc = null)
            : base(d, dc)
        {
            forward_ = forward;
            param_ = sviParameters;
            init();
        }

        public override double? atmLevel() => forward_;

        public override double maxStrike() => double.MaxValue;

        public override double minStrike() => 0.0;

        protected override double volatilityImpl(double strike)
        {
            var k = System.Math.Log(System.Math.Max(strike, 1E-6) / forward_);
            var totalVariance = Termstructures.Volatility.Utils.sviTotalVariance(param_[0], param_[1], param_[2],
                param_[3], param_[4], k);
            return System.Math.Sqrt(System.Math.Max(0.0, totalVariance / exerciseTime()));
        }

        #region svi smile section

        protected double forward_;
        protected List<double> param_;

        public void init()
        {
            QLNet.Utils.QL_REQUIRE(param_.Count == 5,
                () => "svi expects 5 parameters (a,b,sigma,rho,s,m) but ("
                      + param_.Count + ") given");

            Termstructures.Volatility.Utils.checkSviParameters(param_[0], param_[1], param_[2], param_[3], param_[4]);
        }

        #endregion
    }
}
