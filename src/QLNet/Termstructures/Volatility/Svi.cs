﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

namespace QLNet.Termstructures.Volatility
{
    public partial class Utils
    {
        public static void checkSviParameters(double a, double b, double sigma, double rho, double m)
        {
            QLNet.Utils.QL_REQUIRE(b >= 0.0, () => "b (" + b + ") must be non negative");
            QLNet.Utils.QL_REQUIRE(System.Math.Abs(rho) < 1.0, () => "rho (" + rho + ") must be in (-1,1)");
            QLNet.Utils.QL_REQUIRE(sigma > 0.0, () => "sigma (" + sigma + ") must be positive");
            QLNet.Utils.QL_REQUIRE(a + b * sigma * System.Math.Sqrt(1.0 - rho * rho) >= 0.0,
                () => "a + b sigma sqrt(1-rho^2) (a=" + a + ", b=" + b + ", sigma="
                      + sigma + ", rho=" + rho
                      + ") must be non negative");
            QLNet.Utils.QL_REQUIRE(b * (1.0 + System.Math.Abs(rho)) < 4.0,
                () => "b(1+|rho|) must be less than 4");
        }

        public static double sviTotalVariance(double a, double b, double sigma, double rho, double m, double k) => a + b * (rho * (k - m) + System.Math.Sqrt((k - m) * (k - m) + sigma * sigma));

        public static double sviVolatility(double strike, double forward, double expiryTime, List<double?> param)
        {
            var params_ = new List<double>();
            foreach (var x in param)
            {
                params_.Add(x.Value);
            }

            var sms = new SviSmileSection(expiryTime, forward, params_);
            return sms.volatility(strike);
        }
    }
}
