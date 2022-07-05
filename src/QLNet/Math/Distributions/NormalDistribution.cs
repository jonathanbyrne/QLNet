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

using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    //! Normal distribution function
    /*! Given x, it returns its probability in a Gaussian normal distribution.
        It provides the first derivative too.

        \test the correctness of the returned value is tested by
              checking it against numerical calculations. Cross-checks
              are also performed against the
              CumulativeNormalDistribution and InverseCumulativeNormal
              classes.
    */
    [PublicAPI]
    public class NormalDistribution : IValue
    {
        private double average_, sigma_, normalizationFactor_, denominator_, derNormalizationFactor_;

        public NormalDistribution() : this(0.0, 1.0)
        {
        }

        public NormalDistribution(double average, double sigma)
        {
            average_ = average;
            sigma_ = sigma;

            QLNet.Utils.QL_REQUIRE(sigma_ > 0.0, () => "sigma must be greater than 0.0 (" + sigma_ + " not allowed)");

            normalizationFactor_ = Const.M_SQRT_2 * Const.M_1_SQRTPI / sigma_;
            derNormalizationFactor_ = sigma_ * sigma_;
            denominator_ = 2.0 * derNormalizationFactor_;
        }

        public double derivative(double x) => value(x) * (average_ - x) / derNormalizationFactor_;

        // function
        public double value(double x)
        {
            var deltax = x - average_;
            var exponent = -(deltax * deltax) / denominator_;
            // debian alpha had some strange problem in the very-low range
            return exponent <= -690.0
                ? 0.0
                : // exp(x) < 1.0e-300 anyway
                normalizationFactor_ * System.Math.Exp(exponent);
        }
    }

    //! Cumulative normal distribution function
    /*! Given x it provides an approximation to the
        integral of the gaussian normal distribution:
        formula here ...

        For this implementation see M. Abramowitz and I. Stegun,
        Handbook of Mathematical Functions,
        Dover Publications, New York (1972)
    */

    //! Inverse cumulative normal distribution function
    /*! Given x between zero and one as
      the integral value of a gaussian normal distribution
      this class provides the value y such that
      formula here ...

      It use Acklam's approximation:
      by Peter J. Acklam, University of Oslo, Statistics Division.
      URL: http://home.online.no/~pjacklam/notes/invnorm/index.html

      This class can also be used to generate a gaussian normal
      distribution from a uniform distribution.
      This is especially useful when a gaussian normal distribution
      is generated from a low discrepancy uniform distribution:
      in this case the traditional Box-Muller approach and its
      variants would not preserve the sequence's low-discrepancy.

    */

    //! Moro Inverse cumulative normal distribution class
    /*! Given x between zero and one as
        the integral value of a gaussian normal distribution
        this class provides the value y such that
        formula here ...

        It uses Beasly and Springer approximation, with an improved
        approximation for the tails. See Boris Moro,
        "The Full Monte", 1995, Risk Magazine.

        This class can also be used to generate a gaussian normal
        distribution from a uniform distribution.
        This is especially useful when a gaussian normal distribution
        is generated from a low discrepancy uniform distribution:
        in this case the traditional Box-Muller approach and its
        variants would not preserve the sequence's low-discrepancy.

        Peter J. Acklam's approximation is better and is available
        as QuantLib::InverseCumulativeNormal
    */
}
