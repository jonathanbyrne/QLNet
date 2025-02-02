﻿/*
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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [PublicAPI]
    public class InterpolatedZeroInflationCurve<Interpolator> : ZeroInflationTermStructure, InterpolatedCurve
        where Interpolator : class, IInterpolationFactory, new()
    {
        public InterpolatedZeroInflationCurve(Date referenceDate, Calendar calendar, DayCounter dayCounter, Period lag,
            Frequency frequency, bool indexIsInterpolated, Handle<YieldTermStructure> yTS,
            List<Date> dates, List<double> rates,
            Interpolator interpolator = default)
            : base(referenceDate, calendar, dayCounter, rates[0],
                lag, frequency, indexIsInterpolated, yTS)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = rates;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();

            QLNet.Utils.QL_REQUIRE(dates_.Count > 1, () => "too few dates: " + dates_.Count);

            // check that the data starts from the beginning,
            // i.e. referenceDate - lag, at least must be in the relevant
            // period
            var lim = Utils.inflationPeriod(yTS.link.referenceDate() - observationLag(), frequency);
            QLNet.Utils.QL_REQUIRE(lim.Key <= dates_[0] && dates_[0] <= lim.Value, () =>
                "first data date is not in base period, date: " + dates_[0]
                                                                + " not within [" + lim.Key + "," + lim.Value + "]");

            // by convention, if the index is not interpolated we pull all the dates
            // back to the start of their inflationPeriods
            // otherwise the time calculations will be inconsistent
            if (!indexIsInterpolated_)
            {
                for (var i = 0; i < dates_.Count; i++)
                {
                    dates_[i] = Utils.inflationPeriod(dates_[i], frequency).Key;
                }
            }

            QLNet.Utils.QL_REQUIRE(data_.Count == dates_.Count, () =>
                "indices/dates count mismatch: "
                + data_.Count + " vs " + dates_.Count);

            times_ = new InitializedList<double>(dates_.Count);
            times_[0] = timeFromReference(dates_[0]);
            for (var i = 1; i < dates_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(dates_[i] > dates_[i - 1], () => "dates not sorted");

                // but must be greater than -1
                QLNet.Utils.QL_REQUIRE(data_[i] > -1.0, () => "zero inflation data < -100 %");

                // this can be negative
                times_[i] = timeFromReference(dates_[i]);
                QLNet.Utils.QL_REQUIRE(!Math.Utils.close(times_[i], times_[i - 1]), () =>
                    "two dates correspond to the same time " +
                    "under this curve's day count convention");
            }

            interpolation_ = interpolator_.interpolate(times_, times_.Count, data_);
            interpolation_.update();
        }

        /*! Protected version for use when descendents don't want to
            (or can't) provide the points for interpolation on
            construction.
        */
        protected InterpolatedZeroInflationCurve(Date referenceDate,
            Calendar calendar,
            DayCounter dayCounter,
            Period lag,
            Frequency frequency,
            bool indexIsInterpolated,
            double baseZeroRate,
            Handle<YieldTermStructure> yTS,
            Interpolator interpolator = default)
            : base(referenceDate, calendar, dayCounter, baseZeroRate, lag, frequency, indexIsInterpolated, yTS)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        // InflationTermStructure interface
        public override Date baseDate() =>
            // if indexIsInterpolated we fixed the dates in the constructor
            dates_.First();

        // Inspectors
        public List<double> rates() => data_;

        // ZeroInflationTermStructure Interface
        protected override double zeroRateImpl(double t) => interpolation_.value(t, true);

        #region InterpolatedCurve

        public List<double> times_ { get; set; }

        public virtual List<double> times() => times_;

        public List<Date> dates_ { get; set; }

        public virtual List<Date> dates() => dates_;

        public Date maxDate_ { get; set; }

        public override Date maxDate()
        {
            Date d;
            if (indexIsInterpolated())
            {
                d = dates_.Last();
            }
            else
            {
                d = Utils.inflationPeriod(dates_.Last(), frequency()).Value;
            }

            return d;
        }

        public List<double> data_ { get; set; }

        public List<double> forwards() => data_;

        public virtual List<double> data() => forwards();

        public Interpolation interpolation_ { get; set; }

        public IInterpolationFactory interpolator_ { get; set; }

        public virtual Dictionary<Date, double> nodes()
        {
            var results = new Dictionary<Date, double>();
            dates_.ForEach((i, x) => results.Add(x, data_[i]));
            return results;
        }

        public void setupInterpolation()
        {
            interpolation_ = interpolator_.interpolate(times_, times_.Count, data_);
        }

        public object Clone()
        {
            var copy = MemberwiseClone() as InterpolatedCurve;
            copy.times_ = new List<double>(times_);
            copy.data_ = new List<double>(data_);
            copy.interpolator_ = interpolator_;
            copy.setupInterpolation();
            return copy;
        }

        #endregion
    }
}
