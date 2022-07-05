﻿/*
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

namespace QLNet.Termstructures.Volatility.Inflation
{
    /*! Abstract interface ... no data, only results.

     Basically used to change the BlackVariance() methods to
     totalVariance.  Also deal with lagged observations of an index
     with a (usually different) availability lag.
     */

    public abstract class YoYOptionletVolatilitySurface : VolatilityTermStructure
    {
        protected double? baseLevel_;
        protected Frequency frequency_;
        protected bool indexIsInterpolated_;

        // so you do not need an index
        protected Period observationLag_;

        // Constructor
        //! calculate the reference date based on the global evaluation date
        protected YoYOptionletVolatilitySurface(int settlementDays,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated)
            : base(settlementDays, cal, bdc, dc)
        {
            baseLevel_ = null;
            observationLag_ = observationLag;
            frequency_ = frequency;
            indexIsInterpolated_ = indexIsInterpolated;
        }

        public virtual Date baseDate()
        {
            // Depends on interpolation, or not, of observed index
            // and observation lag with which it was built.
            // We want this to work even if the index does not
            // have a yoy term structure.
            if (indexIsInterpolated())
            {
                return referenceDate() - observationLag();
            }

            return Utils.inflationPeriod(referenceDate() - observationLag(),
                frequency()).Key;
        }

        // acts as zero time value for boostrapping
        public virtual double baseLevel()
        {
            Utils.QL_REQUIRE(baseLevel_ != null, () => "Base volatility, for baseDate(), not set.");
            return baseLevel_.Value;
        }

        public virtual Frequency frequency() => frequency_;

        public virtual bool indexIsInterpolated() => indexIsInterpolated_;

        //! The TS observes with a lag that is usually different from the
        //! availability lag of the index.  An inflation rate is given,
        //! by default, for the maturity requested assuming this lag.
        public virtual Period observationLag() => observationLag_;

        //! base date will be in the past because of observation lag
        public virtual double timeFromBase(Date maturityDate) => timeFromBase(maturityDate, new Period(-1, TimeUnit.Days));

        //! needed for total variance calculations
        public virtual double timeFromBase(Date maturityDate, Period obsLag)
        {
            var useLag = obsLag;
            if (obsLag == new Period(-1, TimeUnit.Days))
            {
                useLag = observationLag();
            }

            Date useDate;
            if (indexIsInterpolated())
            {
                useDate = maturityDate - useLag;
            }
            else
            {
                useDate = Utils.inflationPeriod(maturityDate - useLag, frequency()).Key;
            }

            // This assumes that the inflation term structure starts
            // as late as possible given the inflation index definition,
            // which is the usual case.
            return dayCounter().yearFraction(baseDate(), useDate);
        }

        //! Returns the total integrated variance for a given exercise date and strike rate.
        /*! Total integrated variance is useful because it scales out
         t for the optionlet pricing formulae.  Note that it is
         called "total" because the surface does not know whether
         it represents Black, Bachelier or Displaced Diffusion
         variance.  These are virtual so alternate connections
         between const vol and total var are possible.

         Because inflation is highly linked to dates (for interpolation, periods, etc)
         we do NOT provide a time version
         */
        public virtual double totalVariance(Date maturityDate, double strike) => totalVariance(maturityDate, strike, new Period(-1, TimeUnit.Days), false);

        public virtual double totalVariance(Date maturityDate, double strike, Period obsLag) => totalVariance(maturityDate, strike, obsLag, false);

        public virtual double totalVariance(Date maturityDate, double strike, Period obsLag, bool extrapolate)
        {
            var vol = volatility(maturityDate, strike, obsLag, extrapolate);
            var t = timeFromBase(maturityDate, obsLag);
            return vol * vol * t;
        }

        public virtual double totalVariance(Period tenor, double strike)
        {
            var maturityDate = optionDateFromTenor(tenor);
            return totalVariance(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
        }

        public virtual double totalVariance(Period tenor, double strike, Period obsLag)
        {
            var maturityDate = optionDateFromTenor(tenor);
            return totalVariance(maturityDate, strike, obsLag, false);
        }

        public virtual double totalVariance(Period tenor, double strike, Period obsLag, bool extrap)
        {
            var maturityDate = optionDateFromTenor(tenor);
            return totalVariance(maturityDate, strike, obsLag, extrap);
        }

        // Volatility (only)
        //! Returns the volatility for a given maturity date and strike rate
        //! that observes inflation, by default, with the observation lag
        //! of the term structure.
        //! Because inflation is highly linked to dates (for interpolation, periods, etc)
        //! we do NOT provide a time version.
        public double volatility(Date maturityDate, double strike) => volatility(maturityDate, strike, new Period(-1, TimeUnit.Days), false);

        public double volatility(Date maturityDate, double strike, Period obsLag) => volatility(maturityDate, strike, obsLag, false);

        public double volatility(Date maturityDate, double strike, Period obsLag, bool extrapolate)
        {
            var useLag = obsLag;
            if (obsLag == new Period(-1, TimeUnit.Days))
            {
                useLag = observationLag();
            }

            if (indexIsInterpolated())
            {
                checkRange(maturityDate - useLag, strike, extrapolate);
                var t = timeFromReference(maturityDate - useLag);
                return volatilityImpl(t, strike);
            }
            else
            {
                var dd = Utils.inflationPeriod(maturityDate - useLag, frequency());
                checkRange(dd.Key, strike, extrapolate);
                var t = timeFromReference(dd.Key);
                return volatilityImpl(t, strike);
            }
        }

        public double volatility(Period optionTenor, double strike)
        {
            var maturityDate = optionDateFromTenor(optionTenor);
            return volatility(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
        }

        public double volatility(Period optionTenor, double strike, Period obsLag)
        {
            var maturityDate = optionDateFromTenor(optionTenor);
            return volatility(maturityDate, strike, obsLag, false);
        }

        public double volatility(Period optionTenor, double strike, Period obsLag, bool extrapolate)
        {
            var maturityDate = optionDateFromTenor(optionTenor);
            return volatility(maturityDate, strike, obsLag, extrapolate);
        }

        //! Implements the actual volatility surface calculation in
        //! derived classes e.g. bilinear interpolation.  N.B. does
        //! not derive the surface.
        protected abstract double volatilityImpl(double length, double strike);

        protected virtual void checkRange(Date d, double strike, bool extrapolate)
        {
            Utils.QL_REQUIRE(d >= baseDate(), () => "date (" + d + ") is before base date");

            Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                "date (" + d + ") is past max curve date (" + maxDate() + ")");

            Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || strike >= minStrike() && strike <= maxStrike(), () =>
                "strike (" + strike + ") is outside the curve domain [" + minStrike() + "," + maxStrike() + "]] at date = " + d);
        }

        protected virtual void checkRange(double t, double strike, bool extrapolate)
        {
            Utils.QL_REQUIRE(t >= timeFromReference(baseDate()), () => "time (" + t + ") is before base date");

            Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || t <= maxTime(), () =>
                "time (" + t + ") is past max curve time (" + maxTime() + ")");

            Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || strike >= minStrike() && strike <= maxStrike(), () =>
                "strike (" + strike + ") is outside the curve domain [" + minStrike() + "," + maxStrike() + "] at time = " + t);
        }

        // acts as zero time value for boostrapping
        protected virtual void setBaseLevel(double? v)
        {
            baseLevel_ = v;
        }
    }

    //! Constant surface, no K or T dependence.
}
