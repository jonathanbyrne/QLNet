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
using QLNet.Extensions;
using QLNet.Time;
using System;

namespace QLNet.Termstructures.Volatility.equityfx
{
    //! Black-volatility term structure
    /*! This abstract class defines the interface of concrete
       Black-volatility term structures which will be derived from
       this one.

       Volatilities are assumed to be expressed on an annual basis.
    */
    public abstract class BlackVolTermStructure : VolatilityTermStructure
    {
        #region Constructors
        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */

        protected BlackVolTermStructure(BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
           : base(bdc, dc)
        { }

        //! initialize with a fixed reference date
        protected BlackVolTermStructure(Date referenceDate, Calendar cal = null,
                                        BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
           : base(referenceDate, cal, bdc, dc)
        { }

        //! calculate the reference date based on the global evaluation date
        protected BlackVolTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc = BusinessDayConvention.Following,
                                        DayCounter dc = null)
           : base(settlementDays, cal, bdc, dc)
        { }

        #endregion

        #region Black Volatility

        //! spot volatility
        public double blackVol(Date maturity, double strike, bool extrapolate = false)
        {
            checkRange(maturity, extrapolate);
            checkStrike(strike, extrapolate);
            var t = timeFromReference(maturity);
            return blackVolImpl(t, strike);
        }

        //! spot volatility
        public double blackVol(double maturity, double strike, bool extrapolate = false)
        {
            checkRange(maturity, extrapolate);
            checkStrike(strike, extrapolate);
            return blackVolImpl(maturity, strike);
        }

        //! spot variance
        public double blackVariance(Date maturity, double strike, bool extrapolate = false)
        {
            checkRange(maturity, extrapolate);
            checkStrike(strike, extrapolate);
            var t = timeFromReference(maturity);
            return blackVarianceImpl(t, strike);
        }

        //! spot variance
        public double blackVariance(double maturity, double strike, bool extrapolate = false)
        {
            checkRange(maturity, extrapolate);
            checkStrike(strike, extrapolate);
            return blackVarianceImpl(maturity, strike);
        }

        //! forward (at-the-money) volatility
        public double blackForwardVol(Date date1, Date date2, double strike, bool extrapolate = false)
        {
            // (redundant) date-based checks
            Utils.QL_REQUIRE(date1 <= date2, () => date1 + " later than " + date2);
            checkRange(date2, extrapolate);

            // using the time implementation
            var time1 = timeFromReference(date1);
            var time2 = timeFromReference(date2);
            return blackForwardVol(time1, time2, strike, extrapolate);
        }

        //! forward (at-the-money) volatility
        public double blackForwardVol(double time1, double time2, double strike, bool extrapolate = false)
        {
            Utils.QL_REQUIRE(time1 <= time2, () => time1 + " later than " + time2);
            checkRange(time2, extrapolate);
            checkStrike(strike, extrapolate);
            if (time2.IsEqual(time1))
            {
                if (time1.IsEqual(0.0))
                {
                    var epsilon = 1.0e-5;
                    var var = blackVarianceImpl(epsilon, strike);
                    return System.Math.Sqrt(var / epsilon);
                }
                else
                {
                    var epsilon = System.Math.Min(1.0e-5, time1);
                    var var1 = blackVarianceImpl(time1 - epsilon, strike);
                    var var2 = blackVarianceImpl(time1 + epsilon, strike);
                    Utils.QL_REQUIRE(var2 >= var1, () => "variances must be non-decreasing");
                    return System.Math.Sqrt((var2 - var1) / (2 * epsilon));
                }
            }
            else
            {
                var var1 = blackVarianceImpl(time1, strike);
                var var2 = blackVarianceImpl(time2, strike);
                Utils.QL_REQUIRE(var2 >= var1, () => "variances must be non-decreasing");
                return System.Math.Sqrt((var2 - var1) / (time2 - time1));
            }
        }

        //! forward (at-the-money) variance
        public double blackForwardVariance(Date date1, Date date2, double strike, bool extrapolate = false)
        {
            // (redundant) date-based checks
            Utils.QL_REQUIRE(date1 <= date2, () => date1 + " later than " + date2);
            checkRange(date2, extrapolate);

            // using the time implementation
            var time1 = timeFromReference(date1);
            var time2 = timeFromReference(date2);
            return blackForwardVariance(time1, time2, strike, extrapolate);
        }

        //! forward (at-the-money) variance
        public double blackForwardVariance(double time1, double time2, double strike, bool extrapolate = false)
        {
            Utils.QL_REQUIRE(time1 <= time2, () => time1 + " later than " + time2);
            checkRange(time2, extrapolate);
            checkStrike(strike, extrapolate);
            var v1 = blackVarianceImpl(time1, strike);
            var v2 = blackVarianceImpl(time2, strike);
            Utils.QL_REQUIRE(v2 >= v1, () => "variances must be non-decreasing");
            return v2 - v1;
        }

        #endregion

        #region Calculations

        //   These methods must be implemented in derived classes to perform
        //   the actual volatility calculations. When they are called,
        //   range check has already been performed; therefore, they must
        //   assume that extrapolation is required.

        //! Black variance calculation
        protected abstract double blackVarianceImpl(double t, double strike);

        //! Black volatility calculation
        protected abstract double blackVolImpl(double t, double strike);

        #endregion

    }

    //! Black-volatility term structure
    /*! This abstract class acts as an adapter to BlackVolTermStructure
        allowing the programmer to implement only the
        <tt>blackVolImpl(Time, Real, bool)</tt> method in derived classes.

        Volatility are assumed to be expressed on an annual basis.
    */

    //! Black variance term structure
    /*! This abstract class acts as an adapter to VolTermStructure allowing
        the programmer to implement only the
        <tt>blackVarianceImpl(Time, Real, bool)</tt> method in derived
        classes.

        Volatility are assumed to be expressed on an annual basis.
    */
}
