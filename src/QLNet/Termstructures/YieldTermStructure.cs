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

using System.Collections.Generic;
using QLNet.Extensions;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures
{
    //! Interest-rate term structure
    /*! This abstract class defines the interface of concrete
       interest rate structures which will be derived from this one.

       \ingroup yieldtermstructures

       \test observability against evaluation date changes is checked.
    */
    public abstract class YieldTermStructure : TermStructure
    {
        private const double dt = 0.0001;
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

        #region Calculations

        //    This method must be implemented in derived classes to
        //    perform the actual calculations. When it is called,
        //    range check has already been performed; therefore, it
        //    must assume that extrapolation is required.

        //! discount factor calculation
        protected abstract double discountImpl(double d);

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

        protected YieldTermStructure(DayCounter dc = null, List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
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

        protected YieldTermStructure(Date referenceDate, Calendar cal = null, DayCounter dc = null,
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

        protected YieldTermStructure(int settlementDays, Calendar cal, DayCounter dc = null,
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

        #region Discount factors

        //    These methods return the discount factor from a given date or time
        //    to the reference date.  In the latter case, the time is calculated
        //    as a fraction of year from the reference date.

        public double discount(Date d, bool extrapolate = false) => discount(timeFromReference(d), extrapolate);

        /*! The same day-counting rule used by the term structure
            should be used for calculating the passed time t.
        */
        public double discount(double t, bool extrapolate = false)
        {
            checkRange(t, extrapolate);

            if (jumps_.empty())
            {
                return discountImpl(t);
            }

            var jumpEffect = 1.0;
            for (var i = 0; i < nJumps_; ++i)
            {
                if (jumpTimes_[i] > 0 && jumpTimes_[i] < t)
                {
                    QLNet.Utils.QL_REQUIRE(jumps_[i].link.isValid(), () => "invalid " + (i + 1) + " jump quote");
                    var thisJump = jumps_[i].link.value();
                    QLNet.Utils.QL_REQUIRE(thisJump > 0.0, () => "invalid " + (i + 1) + " jump value: " + thisJump);
#if !QL_NEGATIVE_RATES
               QLNet.Utils.QL_REQUIRE(thisJump <= 1.0, () => "invalid " + (i + 1) + " jump value: " + thisJump);
#endif
                    jumpEffect *= thisJump;
                }
            }

            return jumpEffect * discountImpl(t);
        }

        #endregion

        #region Zero-yield rates

        //    These methods return the implied zero-yield rate for a
        //    given date or time.  In the former case, the time is
        //    calculated as a fraction of year from the reference date.

        /*! The resulting interest rate has the required daycounting
            rule.
        */
        public InterestRate zeroRate(Date d, DayCounter dayCounter, Compounding comp, Frequency freq = Frequency.Annual,
            bool extrapolate = false)
        {
            if (d == referenceDate())
            {
                var compound = 1.0 / discount(dt, extrapolate);
                // t has been calculated with a possibly different daycounter
                // but the difference should not matter for very small times
                return InterestRate.impliedRate(compound, dayCounter, comp, freq, dt);
            }

            var compound1 = 1.0 / discount(d, extrapolate);
            return InterestRate.impliedRate(compound1, dayCounter, comp, freq, referenceDate(), d);
        }

        /*! The resulting interest rate has the same day-counting rule
            used by the term structure. The same rule should be used
            for calculating the passed time t.
        */
        public InterestRate zeroRate(double t, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            if (t.IsEqual(0.0))
            {
                t = dt;
            }

            var compound = 1.0 / discount(t, extrapolate);
            return InterestRate.impliedRate(compound, dayCounter(), comp, freq, t);
        }

        #endregion

        #region Forward rates

        //    These methods returns the forward interest rate between two dates
        //    or times.  In the former case, times are calculated as fractions
        //    of year from the reference date.
        //
        //    If both dates (times) are equal the instantaneous forward rate is
        //    returned.

        /*! The resulting interest rate has the required day-counting
            rule.
        */
        public InterestRate forwardRate(Date d1, Date d2, DayCounter dayCounter, Compounding comp,
            Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            if (d1 == d2)
            {
                checkRange(d1, extrapolate);
                var t1 = System.Math.Max(timeFromReference(d1) - dt / 2.0, 0.0);
                var t2 = t1 + dt;
                var compound = discount(t1, true) / discount(t2, true);
                // times have been calculated with a possibly different daycounter
                // but the difference should not matter for very small times
                return InterestRate.impliedRate(compound, dayCounter, comp, freq, dt);
            }

            QLNet.Utils.QL_REQUIRE(d1 < d2, () => d1 + " later than " + d2);
            var compound1 = discount(d1, extrapolate) / discount(d2, extrapolate);
            return InterestRate.impliedRate(compound1, dayCounter, comp, freq, d1, d2);
        }

        /*! The resulting interest rate has the required day-counting
            rule.
            \warning dates are not adjusted for holidays
        */
        public InterestRate forwardRate(Date d, Period p, DayCounter dayCounter, Compounding comp,
            Frequency freq = Frequency.Annual, bool extrapolate = false) =>
            forwardRate(d, d + p, dayCounter, comp, freq, extrapolate);

        /*! The resulting interest rate has the same day-counting rule
            used by the term structure. The same rule should be used
            for calculating the passed times t1 and t2.
        */
        public InterestRate forwardRate(double t1, double t2, Compounding comp, Frequency freq = Frequency.Annual,
            bool extrapolate = false)
        {
            double compound;
            if (t2.IsEqual(t1))
            {
                checkRange(t1, extrapolate);
                t1 = System.Math.Max(t1 - dt / 2.0, 0.0);
                t2 = t1 + dt;
                compound = discount(t1, true) / discount(t2, true);
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(t2 > t1, () => "t2 (" + t2 + ") < t1 (" + t2 + ")");
                compound = discount(t1, extrapolate) / discount(t2, extrapolate);
            }

            return InterestRate.impliedRate(compound, dayCounter(), comp, freq, t2 - t1);
        }

        #endregion

        #region Jump inspectors

        public List<Date> jumpDates() => jumpDates_;

        public List<double> jumpTimes() => jumpTimes_;

        #endregion
    }
}
