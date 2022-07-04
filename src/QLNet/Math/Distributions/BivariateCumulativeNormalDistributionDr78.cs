/*
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Math.Distributions
{
    //! Cumulative bivariate normal distribution function
    /*! Drezner (1978) algorithm, six decimal places accuracy.

        For this implementation see
       "Option pricing formulas", E.G. Haug, McGraw-Hill 1998

        \todo check accuracy of this algorithm and compare with:
              1) Drezner, Z, (1978),
                 Computation of the bivariate normal integral,
                 Mathematics of Computation 32, pp. 277-279.
              2) Drezner, Z. and Wesolowsky, G. O. (1990)
                 `On the Computation of the Bivariate Normal Integral',
                 Journal of Statistical Computation and Simulation 35,
                 pp. 101-107.
              3) Drezner, Z (1992)
                 Computation of the Multivariate Normal Integral,
                 ACM Transactions on Mathematics Software 18, pp. 450-460.
              4) Drezner, Z (1994)
                 Computation of the Trivariate Normal Integral,
                 Mathematics of Computation 62, pp. 289-294.
              5) Genz, A. (1992)
                `Numerical Computation of the Multivariate Normal
                 Probabilities', J. Comput. Graph. Stat. 1, pp. 141-150.

        \test the correctness of the returned value is tested by
              checking it against known good results.
    */

    [JetBrains.Annotations.PublicAPI] public class BivariateCumulativeNormalDistributionDr78
    {
        public BivariateCumulativeNormalDistributionDr78(double rho)
        {
            rho_ = rho;
            rho2_ = rho * rho;

            Utils.QL_REQUIRE(rho >= -1.0, () => "rho must be >= -1.0 (" + rho + " not allowed)");
            Utils.QL_REQUIRE(rho <= 1.0, () => "rho must be <= 1.0 (" + rho + " not allowed)");
        }

        // function
        public double value(double a, double b)
        {
            var cumNormalDist = new CumulativeNormalDistribution();
            var CumNormDistA = cumNormalDist.value(a);
            var CumNormDistB = cumNormalDist.value(b);
            var MaxCumNormDistAB = System.Math.Max(CumNormDistA, CumNormDistB);
            var MinCumNormDistAB = System.Math.Min(CumNormDistA, CumNormDistB);

            if (1.0 - MaxCumNormDistAB < 1e-15)
                return MinCumNormDistAB;

            if (MinCumNormDistAB < 1e-15)
                return MinCumNormDistAB;

            var a1 = a / System.Math.Sqrt(2.0 * (1.0 - rho2_));
            var b1 = b / System.Math.Sqrt(2.0 * (1.0 - rho2_));

            var result = -1.0;

            if (a <= 0.0 && b <= 0 && rho_ <= 0)
            {
                var sum = 0.0;
                for (var i = 0; i < 5; i++)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        sum += x_[i] * x_[j] * System.Math.Exp(a1 * (2.0 * y_[i] - a1) + b1 * (2.0 * y_[j] - b1) + 2.0 * rho_ * (y_[i] - a1) * (y_[j] - b1));
                    }
                }
                result = System.Math.Sqrt(1.0 - rho2_) / Const.M_PI * sum;
            }
            else if (a <= 0 && b >= 0 && rho_ >= 0)
            {
                var bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(-rho_);
                result = CumNormDistA - bivCumNormalDist.value(a, -b);
            }
            else if (a >= 0.0 && b <= 0.0 && rho_ >= 0.0)
            {
                var bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(-rho_);
                result = CumNormDistB - bivCumNormalDist.value(-a, b);
            }
            else if (a >= 0.0 && b >= 0.0 && rho_ <= 0.0)
            {
                result = CumNormDistA + CumNormDistB - 1.0 + value(-a, -b);
            }
            else if (a * b * rho_ > 0.0)
            {
                var rho1 = (rho_ * a - b) * (a > 0.0 ? 1.0 : -1.0) / System.Math.Sqrt(a * a - 2.0 * rho_ * a * b + b * b);
                var bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(rho1);

                var rho2 = (rho_ * b - a) * (b > 0.0 ? 1.0 : -1.0) / System.Math.Sqrt(a * a - 2.0 * rho_ * a * b + b * b);
                var CBND2 = new BivariateCumulativeNormalDistributionDr78(rho2);

                var delta = (1.0 - (a > 0.0 ? 1.0 : -1.0) * (b > 0.0 ? 1.0 : -1.0)) / 4.0;

                result = bivCumNormalDist.value(a, 0.0) + CBND2.value(b, 0.0) - delta;
            }
            else
            {
                Utils.QL_FAIL("case not handled");
            }

            return result;
        }

        private double rho_, rho2_;
        private static double[] x_ = { 0.24840615,
                                     0.39233107,
                                     0.21141819,
                                     0.03324666,
                                     0.00082485334
                                   };

        private static double[] y_ = { 0.10024215,
                                     0.48281397,
                                     1.06094980,
                                     1.77972940,
                                     2.66976040000
                                   };

    }

    //! Cumulative bivariate normal distibution function (West 2004)
    /*! The implementation derives from the article "Better
       Approximations To Cumulative Normal Distibutions", Graeme
       West, Dec 2004 available at www.finmod.co.za. Also available
       in Wilmott Magazine, 2005, (May), 70-76, The main code is a
       port of the C++ code at www.finmod.co.za/cumfunctions.zip.

       The algorithm is based on the near double-precision algorithm
       described in "Numerical Computation of Rectangular Bivariate
       an Trivariate Normal and t Probabilities", Genz (2004),
       Statistics and Computing 14, 151-160. (available at
       www.sci.wsu.edu/math/faculty/henz/homepage)

       The QuantLib implementation mainly differs from the original
       code in two regards
       - The implementation of the cumulative normal distribution is
          QuantLib::CumulativeNormalDistribution
       - The arrays XX and W are zero-based

       \test the correctness of the returned value is tested by
             checking it against known good results.
    */
}
