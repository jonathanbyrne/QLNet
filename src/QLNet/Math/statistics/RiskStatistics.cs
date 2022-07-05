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

namespace QLNet.Math.statistics
{
    //! empirical-distribution risk measures
    /*! This class wraps a somewhat generic statistic tool and adds
        a number of risk measures (e.g.: value-at-risk, expected
        shortfall, etc.) based on the data distribution as reported by
        the underlying statistic tool.

        \todo add historical annualized volatility
    */

    //! default risk measures tool
    /*! \test the correctness of the returned values is tested by checking them against numerical calculations. */
    [PublicAPI]
    public class RiskStatistics : GenericRiskStatistics<GaussianStatistics>
    {
        public double gaussianAverageShortfall(double value) => impl_.gaussianAverageShortfall(value);

        public double gaussianDownsideVariance() => impl_.gaussianDownsideVariance();

        public double gaussianExpectedShortfall(double value) => impl_.gaussianExpectedShortfall(value);

        public double gaussianPercentile(double value) => impl_.gaussianPercentile(value);

        public double gaussianPotentialUpside(double value) => impl_.gaussianPotentialUpside(value);

        public double gaussianRegret(double value) => impl_.gaussianRegret(value);

        public double gaussianShortfall(double value) => impl_.gaussianShortfall(value);

        public double gaussianValueAtRisk(double value) => impl_.gaussianValueAtRisk(value);
    }

    //! default statistics tool
    /*! \test the correctness of the returned values is tested by checking them against numerical calculations. */
}
