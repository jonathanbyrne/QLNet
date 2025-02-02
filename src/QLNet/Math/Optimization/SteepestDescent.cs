/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    //! Multi-dimensional steepest-descent class
    /*! User has to provide line-search method and optimization end criteria

        search direction \f$ = - f'(x) \f$
    */
    [PublicAPI]
    public class SteepestDescent : LineSearchBasedMethod
    {
        public SteepestDescent(LineSearch lineSearch = null)
            : base(lineSearch)
        {
        }

        protected override Vector getUpdatedDirection(Problem P, double gold2, Vector gradient) => lineSearch_.lastGradient() * -1;
    }
}
