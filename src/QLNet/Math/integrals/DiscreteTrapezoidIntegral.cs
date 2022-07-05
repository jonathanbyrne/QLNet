/*
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

using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    /*! References:
        Levy, D. Numerical Integration
        http://www2.math.umd.edu/~dlevy/classes/amsc466/lecture-notes/integration-chap.pdf
    */
    [PublicAPI]
    public class DiscreteTrapezoidIntegral
    {
        public double value(Vector x, Vector f)
        {
            var n = f.size();
            Utils.QL_REQUIRE(n == x.size(), () => "inconsistent size");

            double acc = 0;

            for (var i = 0; i < n - 1; ++i)
            {
                acc += (x[i + 1] - x[i]) * (f[i] + f[i + 1]);
            }

            return 0.5 * acc;
        }
    }
}
