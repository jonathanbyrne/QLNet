﻿/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
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
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class InterpolatedZeroCurve<Interpolator> : ZeroYieldStructure, InterpolatedCurve
        where Interpolator : class, IInterpolationFactory, new()
    {
        public InterpolatedZeroCurve(DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedZeroCurve(Date referenceDate,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(referenceDate, null, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedZeroCurve(int settlementDays,
            Calendar calendar,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(settlementDays, calendar, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedZeroCurve(List<Date> dates,
            List<double> yields,
            DayCounter dayCounter,
            Calendar calendar = null,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(dates[0], calendar, dayCounter, jumps, jumpDates)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = yields;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
            initialize(compounding, frequency);
        }

        public InterpolatedZeroCurve(List<Date> dates,
            List<double> yields,
            DayCounter dayCounter,
            Calendar calendar,
            Interpolator interpolator,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(dates[0], calendar, dayCounter)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = yields;
            interpolator_ = interpolator;
            initialize(compounding, frequency);
        }

        public InterpolatedZeroCurve(List<Date> dates,
            List<double> yields,
            DayCounter dayCounter,
            Interpolator interpolator,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual,
            Date refDate = null)
            : base(refDate ?? dates[0], null, dayCounter)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = yields;
            interpolator_ = interpolator;
            initialize(compounding, frequency, refDate);
        }

        protected void initialize(Compounding compounding, Frequency frequency, Date refDate = null)
        {
            QLNet.Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints, () => "not enough input dates given");
            QLNet.Utils.QL_REQUIRE(data_.Count == dates_.Count, () => "dates/yields count mismatch");

            times_ = new List<double>(dates_.Count);
            var offset = 0.0;
            if (refDate != null)
            {
                offset = dayCounter().yearFraction(refDate, dates_[0]);
            }

            times_.Add(offset);

            if (compounding != Compounding.Continuous)
            {
                // We also have to convert the first rate.
                // The first time is 0.0, so we can't use it.
                // We fall back to about one day.
                var dt = 1.0 / 365;
                var r = new InterestRate(data_[0], dayCounter(), compounding, frequency);
                data_[0] = r.equivalentRate(Compounding.Continuous, Frequency.NoFrequency, dt).value();
#if !QL_NEGATIVE_RATES
            QLNet.Utils.QL_REQUIRE(data_[0] > 0.0, () => "non-positive yield");
#endif
            }

            for (var i = 1; i < dates_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(dates_[i] > dates_[i - 1], () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
                times_.Add(dayCounter().yearFraction(refDate ?? dates_[0], dates_[i]));

                QLNet.Utils.QL_REQUIRE(!Math.Utils.close(times_[i], times_[i - 1]), () =>
                    "two dates correspond to the same time " +
                    "under this curve's day count convention");

                // adjusting zero rates to match continuous compounding
                if (compounding != Compounding.Continuous)
                {
                    var r = new InterestRate(data_[i], dayCounter(), compounding, frequency);
                    data_[i] = r.equivalentRate(Compounding.Continuous, Frequency.NoFrequency, times_[i]).value();
                }

#if !QL_NEGATIVE_RATES
            QLNet.Utils.QL_REQUIRE(data_[i] > 0.0, () => "non-positive yield");
            // positive yields are not enough to ensure non-negative fwd rates
            // so here's a stronger requirement
            QLNet.Utils.QL_REQUIRE(data_[i] * times_[i] - data_[i - 1] * times_[i - 1] >= 0.0,
                             () => "negative forward rate implied by the zero yield " + data_[i] + " at " + dates_[i] +
                             " (t=" + times_[i] + ") after the zero yield " +
                             data_[i - 1] + " at " + dates_[i - 1] +
                             " (t=" + times_[i - 1] + ")");
#endif
            }

            setupInterpolation();
            interpolation_.update();
        }

        protected override double zeroYieldImpl(double t)
        {
            if (t <= times_.Last())
            {
                return interpolation_.value(t, true);
            }

            // flat fwd extrapolation
            var tMax = times_.Last();
            var zMax = data_.Last();
            var instFwdMax = zMax + tMax * interpolation_.derivative(tMax);
            return (zMax * tMax + instFwdMax * (t - tMax)) / t;
        }

        #region InterpolatedCurve

        public List<double> times_ { get; set; }

        public List<double> times() => times_;

        public List<Date> dates_ { get; set; }

        public List<Date> dates() => dates_;

        public Date maxDate_ { get; set; }

        public override Date maxDate()
        {
            if (maxDate_ != null)
            {
                return maxDate_;
            }

            return dates_.Last();
        }

        public List<double> data_ { get; set; }

        public List<double> zeroRates() => data_;

        public List<double> data() => zeroRates();

        public Interpolation interpolation_ { get; set; }

        public IInterpolationFactory interpolator_ { get; set; }

        public Dictionary<Date, double> nodes()
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
