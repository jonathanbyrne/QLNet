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

namespace QLNet.Math.statistics
{
    //! Statistics tool for gaussian-assumption risk measures
    /*! This class wraps a somewhat generic statistic tool and adds
        a number of gaussian risk measures (e.g.: value-at-risk, expected
        shortfall, etc.) based on the mean and variance provided by
        the underlying statistic tool.
    */

    //! default gaussian statistic tool
    [JetBrains.Annotations.PublicAPI] public class GaussianStatistics : GenericGaussianStatistics<GeneralStatistics> { }


    //! Helper class for precomputed distributions
}
