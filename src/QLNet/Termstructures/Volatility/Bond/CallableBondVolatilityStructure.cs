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

using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Bond
{
    //! Callable-bond volatility structure
    /*! This class is purely abstract and defines the interface of
        concrete callable-bond volatility structures which will be
        derived from this one.
    */
    public abstract class CallableBondVolatilityStructure : TermStructure
    {
        private readonly BusinessDayConvention bdc_;
        //! default constructor
        /*! \warning term structures initialized by means of this
                    constructor must manage their own reference date
                    by overriding the referenceDate() method.
        */

        protected CallableBondVolatilityStructure(DayCounter dc = null, BusinessDayConvention bdc = BusinessDayConvention.Following)
            : base(dc ?? new DayCounter())
        {
            bdc_ = bdc;
        }

        //! initialize with a fixed reference date
        protected CallableBondVolatilityStructure(Date referenceDate, Calendar calendar = null, DayCounter dc = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following)
            : base(referenceDate, calendar ?? new Calendar(), dc ?? new DayCounter())
        {
            bdc_ = bdc;
        }

        //! calculate the reference date based on the global evaluation date
        protected CallableBondVolatilityStructure(int settlementDays, Calendar calendar, DayCounter dc = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following)
            : base(settlementDays, calendar, dc ?? new DayCounter())
        {
            bdc_ = bdc;
        }

        // Limits
        //! the largest length for which the term structure can return vols
        public abstract Period maxBondTenor();

        //! the maximum strike for which the term structure can return vols
        public abstract double maxStrike();

        //! the minimum strike for which the term structure can return vols
        public abstract double minStrike();

        //! returns the Black variance for a given option time and bondLength
        public double blackVariance(double optionTime, double bondLength, double strike, bool extrapolate = false)
        {
            checkRange(optionTime, bondLength, strike, extrapolate);
            var vol = volatilityImpl(optionTime, bondLength, strike);
            return vol * vol * optionTime;
        }

        //! returns the Black variance for a given option date and bond tenor
        public double blackVariance(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
        {
            var vol = volatility(optionDate, bondTenor, strike, extrapolate);
            var p = convertDates(optionDate, bondTenor);
            return vol * vol * p.Key;
        }

        //! returns the Black variance for a given option tenor and bond tenor
        public double blackVariance(Period optionTenor, Period bondTenor, double strike, bool extrapolate = false)
        {
            var optionDate = optionDateFromTenor(optionTenor);
            var vol = volatility(optionDate, bondTenor, strike, extrapolate);
            var p = convertDates(optionDate, bondTenor);
            return vol * vol * p.Key;
        }

        //! the business day convention used for option date calculation
        public virtual BusinessDayConvention businessDayConvention() => bdc_;

        //! implements the conversion between dates and times
        public virtual KeyValuePair<double, double> convertDates(Date optionDate, Period bondTenor)
        {
            var end = optionDate + bondTenor;
            QLNet.Utils.QL_REQUIRE(end > optionDate, () =>
                "negative bond tenor (" + bondTenor + ") given");
            var optionTime = timeFromReference(optionDate);
            var timeLength = dayCounter().yearFraction(optionDate, end);
            return new KeyValuePair<double, double>(optionTime, timeLength);
        }

        //! the largest bondLength for which the term structure can return vols
        public virtual double maxBondLength() => timeFromReference(referenceDate() + maxBondTenor());

        //! implements the conversion between optionTenors and optionDates
        public Date optionDateFromTenor(Period optionTenor) =>
            calendar().advance(referenceDate(),
                optionTenor,
                businessDayConvention());

        public virtual SmileSection smileSection(Date optionDate, Period bondTenor)
        {
            var p = convertDates(optionDate, bondTenor);
            return smileSectionImpl(p.Key, p.Value);
        }

        public SmileSection smileSection(Period optionTenor, Period bondTenor)
        {
            var optionDate = optionDateFromTenor(optionTenor);
            return smileSection(optionDate, bondTenor);
        }

        //! returns the volatility for a given option time and bondLength
        public double volatility(double optionTenor, double bondTenor, double strike, bool extrapolate = false)
        {
            checkRange(optionTenor, bondTenor, strike, extrapolate);
            return volatilityImpl(optionTenor, bondTenor, strike);
        }

        //! returns the volatility for a given option date and bond tenor
        public double volatility(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
        {
            checkRange(optionDate, bondTenor, strike, extrapolate);
            return volatilityImpl(optionDate, bondTenor, strike);
        }

        //! returns the volatility for a given option tenor and bond tenor
        public double volatility(Period optionTenor, Period bondTenor, double strike, bool extrapolate = false)
        {
            var optionDate = optionDateFromTenor(optionTenor);
            return volatility(optionDate, bondTenor, strike, extrapolate);
        }

        //! return smile section
        protected abstract SmileSection smileSectionImpl(double optionTime, double bondLength);

        //! implements the actual volatility calculation in derived classes
        protected abstract double volatilityImpl(double optionTime, double bondLength, double strike);

        protected void checkRange(double optionTime, double bondLength, double k, bool extrapolate)
        {
            checkRange(optionTime, extrapolate);
            QLNet.Utils.QL_REQUIRE(bondLength >= 0.0, () =>
                "negative bondLength (" + bondLength + ") given");
            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                                            bondLength <= maxBondLength(), () =>
                "bondLength (" + bondLength + ") is past max curve bondLength ("
                + maxBondLength() + ")");
            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                                            k >= minStrike() && k <= maxStrike(), () =>
                "strike (" + k + ") is outside the curve domain ["
                + minStrike() + "," + maxStrike() + "]");
        }

        protected void checkRange(Date optionDate, Period bondTenor, double k, bool extrapolate)
        {
            checkRange(timeFromReference(optionDate),
                extrapolate);
            QLNet.Utils.QL_REQUIRE(bondTenor.length() > 0, () =>
                "negative bond tenor (" + bondTenor + ") given");
            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                                            bondTenor <= maxBondTenor(), () =>
                "bond tenor (" + bondTenor + ") is past max tenor ("
                + maxBondTenor() + ")");
            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                                            k >= minStrike() && k <= maxStrike(), () =>
                "strike (" + k + ") is outside the curve domain ["
                + minStrike() + "," + maxStrike() + "]");
        }

        protected virtual double volatilityImpl(Date optionDate, Period bondTenor, double strike)
        {
            var p = convertDates(optionDate, bondTenor);
            return volatilityImpl(p.Key, p.Value, strike);
        }
    }
}
