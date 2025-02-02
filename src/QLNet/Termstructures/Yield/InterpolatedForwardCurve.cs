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
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class InterpolatedForwardCurve<Interpolator> : ForwardRateStructure, InterpolatedCurve
        where Interpolator : class, IInterpolationFactory, new()
    {
        public InterpolatedForwardCurve(DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedForwardCurve(Date referenceDate,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(referenceDate, null, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedForwardCurve(int settlementDays,
            Calendar calendar,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(settlementDays, calendar, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedForwardCurve(List<Date> dates,
            List<double> forwards,
            DayCounter dayCounter,
            Calendar calendar = null,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dates[0], calendar, dayCounter, jumps, jumpDates)
        {
            times_ = new List<double>();
            data_ = forwards;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
            dates_ = dates;
            initialize();
        }

        public InterpolatedForwardCurve(List<Date> dates,
            List<double> forwards,
            DayCounter dayCounter,
            Calendar calendar,
            Interpolator interpolator)
            : base(dates[0], calendar, dayCounter)
        {
            times_ = new List<double>();
            data_ = forwards;
            interpolator_ = interpolator;
            dates_ = dates;
            initialize();
        }

        public InterpolatedForwardCurve(List<Date> dates,
            List<double> forwards,
            DayCounter dayCounter,
            Interpolator interpolator)
            : base(dates[0], null, dayCounter)
        {
            times_ = new List<double>();
            data_ = forwards;
            interpolator_ = interpolator;
            dates_ = dates;
            initialize();
        }

        public InterpolatedForwardCurve(List<Date> dates,
            List<double> forwards,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps,
            List<Date> jumpDates,
            Interpolator interpolator = default)
            : base(dates[0], null, dayCounter, jumps, jumpDates)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = forwards;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
            initialize();
        }

        protected override double forwardImpl(double t)
        {
            if (t <= times_.Last())
            {
                return interpolation_.value(t, true);
            }

            // flat fwd extrapolation
            return data_.Last();
        }

        protected override double zeroYieldImpl(double t)
        {
            if (t.IsEqual(0.0))
            {
                return forwardImpl(0.0);
            }

            double integral;
            if (t <= times_.Last())
            {
                integral = interpolation_.primitive(t, true);
            }
            else
            {
                integral = interpolation_.primitive(times_.Last(), true)
                           + data_.Last() * (t - times_.Last());
            }

            return integral / t;
        }

        private void initialize()
        {
            QLNet.Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints,
                () => "not enough input dates givesn");
            QLNet.Utils.QL_REQUIRE(data_.Count == dates_.Count,
                () => "dates/data count mismatch");

            times_ = new InitializedList<double>(dates_.Count);
            times_[0] = 0.0;
            for (var i = 1; i < dates_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(dates_[i] > dates_[i - 1],
                    () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
                times_[i] = dayCounter().yearFraction(dates_[0], dates_[i]);
                QLNet.Utils.QL_REQUIRE(!Math.Utils.close(times_[i], times_[i - 1]),
                    () => "two dates correspond to the same time " +
                          "under this curve's day count convention");

#if !QL_NEGATIVE_RATES
            QLNet.Utils.QL_REQUIRE(this.data_[i] >= 0.0, () => "negative forward");
#endif
            }

            setupInterpolation();
            interpolation_.update();
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

        public List<double> forwards() => data_;

        public List<double> data() => forwards();

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
