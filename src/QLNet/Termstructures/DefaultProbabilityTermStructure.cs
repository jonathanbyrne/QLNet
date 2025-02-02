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

using System.Collections.Generic;
using QLNet.Extensions;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures
{
    //! Default probability term structure
    /*! This abstract class defines the interface of concrete
       credit structures which will be derived from this one.

       \ingroup defaultprobabilitytermstructures
    */
    public abstract class DefaultProbabilityTermStructure : TermStructure
    {
        private readonly List<Date> jumpDates_;
        // data members
        private readonly List<Handle<Quote>> jumps_;
        private readonly List<double> jumpTimes_;
        private Date latestReference_;
        private readonly int nJumps_;

        #region Observer interface

        public override void update()
        {
            base.update();
            if (referenceDate() != latestReference_)
            {
                setJumps();
            }
        }

        #endregion

        // methods
        private void setJumps()
        {
            if (jumpDates_.empty() && !jumps_.empty())
            {
                // turn of year dates
                jumpDates_.Clear();
                jumpTimes_.Clear();
                var y = referenceDate().year();
                for (var i = 0; i < nJumps_; ++i)
                {
                    jumpDates_.Add(new Date(31, Month.December, y + i));
                }
            }
            else
            {
                // fixed dats
                QLNet.Utils.QL_REQUIRE(jumpDates_.Count == nJumps_, () =>
                    "mismatch between number of jumps (" + nJumps_ +
                    ") and jump dates (" + jumpDates_.Count + ")");
            }

            for (var i = 0; i < nJumps_; ++i)
            {
                jumpTimes_.Add(timeFromReference(jumpDates_[i]));
            }

            latestReference_ = base.referenceDate();
        }

        #region Constructors

        protected DefaultProbabilityTermStructure(DayCounter dc = null, List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
            : base(dc)
        {
            if (jumps != null)
            {
                jumps_ = jumps;
            }
            else
            {
                jumps_ = new List<Handle<Quote>>();
            }

            if (jumpDates != null)
            {
                jumpDates_ = jumpDates;
            }
            else
            {
                jumpDates_ = new List<Date>();
            }

            jumpTimes_ = new List<double>(jumpDates_.Count);
            nJumps_ = jumps_.Count;
            setJumps();
            for (var i = 0; i < nJumps_; ++i)
            {
                jumps_[i].registerWith(update);
            }
        }

        protected DefaultProbabilityTermStructure(Date referenceDate, Calendar cal = null, DayCounter dc = null,
            List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
            : base(referenceDate, cal, dc)
        {
            if (jumps != null)
            {
                jumps_ = jumps;
            }
            else
            {
                jumps_ = new List<Handle<Quote>>();
            }

            if (jumpDates != null)
            {
                jumpDates_ = jumpDates;
            }
            else
            {
                jumpDates_ = new List<Date>();
            }

            jumpTimes_ = new List<double>(jumpDates_.Count);
            nJumps_ = jumps_.Count;
            setJumps();
            for (var i = 0; i < nJumps_; ++i)
            {
                jumps_[i].registerWith(update);
            }
        }

        protected DefaultProbabilityTermStructure(int settlementDays, Calendar cal, DayCounter dc = null,
            List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
            : base(settlementDays, cal, dc)
        {
            if (jumps != null)
            {
                jumps_ = jumps;
            }
            else
            {
                jumps_ = new List<Handle<Quote>>();
            }

            if (jumpDates != null)
            {
                jumpDates_ = jumpDates;
            }
            else
            {
                jumpDates_ = new List<Date>();
            }

            jumpTimes_ = new List<double>(jumpDates_.Count);
            nJumps_ = jumps_.Count;
            setJumps();
            for (var i = 0; i < nJumps_; ++i)
            {
                jumps_[i].registerWith(update);
            }
        }

        #endregion

        #region Survival probabilities

        //      These methods return the survival probability from the reference
        //      date until a given date or time.  In the latter case, the time
        //      is calculated as a fraction of year from the reference date.
        public double survivalProbability(Date d, bool extrapolate = false) => survivalProbability(timeFromReference(d), extrapolate);

        /*! The same day-counting rule used by the term structure
            should be used for calculating the passed time t.
        */
        public double survivalProbability(double t, bool extrapolate = false)
        {
            checkRange(t, extrapolate);

            if (!jumps_.empty())
            {
                var jumpEffect = 1.0;
                for (var i = 0; i < nJumps_ && jumpTimes_[i] < t; ++i)
                {
                    QLNet.Utils.QL_REQUIRE(jumps_[i].link.isValid(), () => "invalid " + (i + 1) + " jump quote");
                    var thisJump = jumps_[i].link.value();
                    QLNet.Utils.QL_REQUIRE(thisJump > 0.0 && thisJump <= 1.0, () => "invalid " + (i + 1) + " jump value: " + thisJump);
                    jumpEffect *= thisJump;
                }

                return jumpEffect * survivalProbabilityImpl(t);
            }

            return survivalProbabilityImpl(t);
        }

        #endregion

        #region Default probabilities

        //    These methods return the default probability from the reference
        //    date until a given date or time.  In the latter case, the time
        //    is calculated as a fraction of year from the reference date.
        public double defaultProbability(Date d, bool extrapolate = false) => 1.0 - survivalProbability(d, extrapolate);

        /*! The same day-counting rule used by the term structure
            should be used for calculating the passed time t.
        */
        public double defaultProbability(double t, bool extrapolate = false) => 1.0 - survivalProbability(t, extrapolate);

        //! probability of default between two given dates
        public double defaultProbability(Date d1, Date d2, bool extrapolate = false)
        {
            QLNet.Utils.QL_REQUIRE(d1 <= d2, () => "initial date (" + d1 + ") later than final date (" + d2 + ")");
            double p1 = d1 < referenceDate() ? 0.0 : defaultProbability(d1, extrapolate), p2 = defaultProbability(d2, extrapolate);
            return p2 - p1;
        }

        //! probability of default between two given times
        public double defaultProbability(double t1, double t2, bool extrapo = false)
        {
            QLNet.Utils.QL_REQUIRE(t1 <= t2, () => "initial time (" + t1 + ") later than final time (" + t2 + ")");
            double p1 = t1 < 0.0 ? 0.0 : defaultProbability(t1, extrapolate), p2 = defaultProbability(t2, extrapolate);
            return p2 - p1;
        }

        #endregion

        #region Default densities

        //    These methods return the default density at a given date or time.
        //    In the latter case, the time is calculated as a fraction of year
        //    from the reference date.

        public double defaultDensity(Date d, bool extrapolate = false) => defaultDensity(timeFromReference(d), extrapolate);

        public double defaultDensity(double t, bool extrapolate = false)
        {
            checkRange(t, extrapolate);
            return defaultDensityImpl(t);
        }

        #endregion

        #region Hazard rates

        //    These methods returns the hazard rate at a given date or time.
        //    In the latter case, the time is calculated as a fraction of year
        //    from the reference date.
        //
        //    Hazard rates are defined with annual frequency and continuous
        //    compounding.

        public double hazardRate(Date d, bool extrapolate = false) => hazardRate(timeFromReference(d), extrapolate);

        public double hazardRate(double t, bool extrapolate = false)
        {
            var S = survivalProbability(t, extrapolate);
            return S.IsEqual(0.0) ? 0.0 : defaultDensity(t, extrapolate) / S;
        }

        #endregion

        #region Jump inspectors

        public List<Date> jumpDates() => jumpDates_;

        public List<double> jumpTimes() => jumpTimes_;

        #endregion

        #region Calculations

        // These methods must be implemented in derived classes to
        // perform the actual calculations. When they are called,
        // range check has already been performed; therefore, they
        // must assume that extrapolation is required.

        //! survival probability calculation
        protected abstract double survivalProbabilityImpl(double t);

        //! default density calculation
        protected abstract double defaultDensityImpl(double t);

        #endregion
    }
}
