/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Currencies;
using QLNet.Patterns;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Time.Calendars;

namespace QLNet.Indexes
{
    //! Base class for inflation-rate indexes,
    [JetBrains.Annotations.PublicAPI] public class InflationIndex : Index, IObserver
    {
        /*! An inflation index may return interpolated
            values.  These are linearly interpolated
            values with act/act convention within a period.
            Note that stored "fixings" are always flat (constant)
            within a period and interpolated as needed.  This
            is because interpolation adds an addional availability
            lag (because you always need the next period to
            give the previous period's value)
            and enables storage of the most recent uninterpolated value.
        */
        public InflationIndex(string familyName,
                              Region region,
                              bool revised,
                              bool interpolated,
                              Frequency frequency,
                              Period availabilitiyLag,
                              Currency currency)
        {
            familyName_ = familyName;
            region_ = region;
            revised_ = revised;
            interpolated_ = interpolated;
            frequency_ = frequency;
            availabilityLag_ = availabilitiyLag;
            currency_ = currency;
            name_ = region_.name() + " " + familyName_;
            Settings.registerWith(update);
            IndexManager.instance().notifier(name()).registerWith(update);
        }


        // Index interface
        public override string name() => name_;

        /*! Inflation indices do not have fixing calendars.  An
            inflation index value is valid for every day (including
            weekends) of a calendar period.  I.e. it uses the
            NullCalendar as its fixing calendar.
        */
        public override Calendar fixingCalendar() => new NullCalendar();

        public override bool isValidFixingDate(Date fixingDate) => true;

        /*! Forecasting index values requires an inflation term
            structure.  The inflation term structure (ITS) defines the
            usual lag (not the index).  I.e.  an ITS is always relatve
            to a base date that is earlier than its asof date.  This
            must be so because indices are available only with a lag.
            However, the index availability lag only sets a minimum
            lag for the ITS.  An ITS may be relative to an earlier
            date, e.g. an index may have a 2-month delay in
            publication but the inflation swaps may take as their base
            the index 3 months before.
        */
        public override double fixing(Date fixingDate, bool forecastTodaysFixing = false) => 0;

        /*! this method creates all the "fixings" for the relevant
            period of the index.  E.g. for monthly indices it will put
            the same value in every calendar day in the month.
        */
        public override void addFixing(Date fixingDate, double fixing, bool forceOverwrite = false)
        {
            var lim = Utils.inflationPeriod(fixingDate, frequency_);
            var n = lim.Value - lim.Key + 1;
            var dates = new List<Date>(n);
            var rates = new List<double>(n);

            for (var i = 0; i < n; ++i)
            {
                dates.Add(lim.Key + i);
                rates.Add(fixing);
            }

            addFixings(dates, rates, forceOverwrite);

        }

        // Observer interface
        public void update() { notifyObservers(); }

        // Inspectors
        public string familyName() => familyName_;

        public Region region() => region_;

        public bool revised() => revised_;

        /*! Forecasting index values using an inflation term structure
           uses the interpolation of the inflation term structure
           unless interpolation is set to false.  In this case the
           extrapolated values are constant within each period taking
           the mid-period extrapolated value.
        */
        public bool interpolated() => interpolated_;

        public Frequency frequency() => frequency_;

        /*! The availability lag describes when the index is
           <i>available</i>, not how it is used.  Specifically the
           fixing for, say, January, may only be available in April
           but the index will always return the index value
           applicable for January as its January fixing (independent
           of the lag in availability).
        */
        public Period availabilityLag() => availabilityLag_;

        public Currency currency() => currency_;

        protected Date referenceDate_;
        protected string familyName_;
        protected Region region_;
        protected bool revised_;
        protected bool interpolated_;
        protected Frequency frequency_;
        protected Period availabilityLag_;
        protected Currency currency_;

        private string name_;
    }


    //! Base class for zero inflation indices.

    //! Base class for year-on-year inflation indices.
    /*! These may be genuine indices published on, say, Bloomberg, or
        "fake" indices that are defined as the ratio of an index at
        different time points.
    */
}
