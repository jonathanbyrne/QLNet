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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    //the first value in the y-vector is ignored.

    #region Helpers

    #endregion

    //! Convex monotone yield-curve interpolation method.
    /*! Enhances implementation of the convex monotone method
        described in "Interpolation Methods for Curve Construction" by
        Hagan & West AMF Vol 13, No2 2006.

        A setting of monotonicity = 1 and quadraticity = 0 will
        reproduce the basic Hagan/West method. However, this can
        produce excessive gradients which can mean P&L swings for some
        curves.  Setting monotonicity < 1 and/or quadraticity > 0
        produces smoother curves.  Extra enhancement to avoid negative
        values (if required) is in place.
    */
    [PublicAPI]
    public class ConvexMonotoneInterpolation : Interpolation
    {
        public ConvexMonotoneInterpolation(List<double> xBegin, int size, List<double> yBegin, double quadraticity,
            double monotonicity, bool forcePositive, bool flatFinalPeriod)
            : this(xBegin, size, yBegin, quadraticity, monotonicity, forcePositive, flatFinalPeriod,
                new Dictionary<double, ISectionHelper>())
        {
        }

        public ConvexMonotoneInterpolation(List<double> xBegin, int size, List<double> yBegin, double quadraticity,
            double monotonicity, bool forcePositive, bool flatFinalPeriod,
            Dictionary<double, ISectionHelper> preExistingHelpers)
        {
            impl_ = new ConvexMonotoneImpl(xBegin, size, yBegin, quadraticity, monotonicity, forcePositive,
                flatFinalPeriod, preExistingHelpers);
            impl_.update();
        }

        public Dictionary<double, ISectionHelper> getExistingHelpers()
        {
            var derived = impl_ as ConvexMonotoneImpl;
            return derived.getExistingHelpers();
        }
    }

    //! Convex-monotone interpolation factory and traits
}
