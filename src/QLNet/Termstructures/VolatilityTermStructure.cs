/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

using QLNet.Time;

namespace QLNet.Termstructures
{
    //! Volatility term structure
    /*! This abstract class defines the interface of concrete
        volatility structures which will be derived from this one.

    */
    public abstract class VolatilityTermStructure : TermStructure
    {
        private readonly BusinessDayConvention bdc_;

        //! the maximum strike for which the term structure can return vols
        public abstract double maxStrike();

        //! the minimum strike for which the term structure can return vols
        public abstract double minStrike();

        //! the business day convention used in tenor to date conversion
        public virtual BusinessDayConvention businessDayConvention() => bdc_;

        //! period/date conversion
        public virtual Date optionDateFromTenor(Period p) =>
            // swaption style
            calendar().advance(referenceDate(), p, businessDayConvention());

        //! strike-range check
        protected void checkStrike(double k, bool extrapolate)
        {
            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                             k >= minStrike() && k <= maxStrike(), () =>
                "strike (" + k + ") is outside the curve domain ["
                + minStrike() + "," + maxStrike() + "]");
        }

        #region Constructors

        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */

        protected VolatilityTermStructure(BusinessDayConvention bdc, DayCounter dc = null)
            : base(dc)
        {
            bdc_ = bdc;
        }

        //! initialize with a fixed reference date
        protected VolatilityTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
            : base(referenceDate, cal, dc)
        {
            bdc_ = bdc;
        }

        //! calculate the reference date based on the global evaluation date
        protected VolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
            : base(settlementDays, cal, dc)
        {
            bdc_ = bdc;
        }

        #endregion
    }
}
