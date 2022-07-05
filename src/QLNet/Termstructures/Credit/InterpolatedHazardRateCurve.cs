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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Credit
{
    [PublicAPI]
    public class InterpolatedHazardRateCurve<Interpolator> : HazardRateStructure, InterpolatedCurve
        where Interpolator : IInterpolationFactory, new()
    {
        public InterpolatedHazardRateCurve(List<Date> dates, List<double> hazardRates, DayCounter dayCounter, Calendar cal = null,
            List<Handle<Quote>> jumps = null, List<Date> jumpDates = null, Interpolator interpolator = default)
            : base(dates[0], cal, dayCounter, jumps, jumpDates)
        {
            dates_ = dates;
            times_ = new List<double>();
            data_ = hazardRates;

            if (interpolator == null)
            {
                interpolator_ = FastActivator<Interpolator>.Create();
            }
            else
            {
                interpolator_ = interpolator;
            }

            initialize();
        }

        public InterpolatedHazardRateCurve(List<Date> dates, List<double> hazardRates, DayCounter dayCounter, Calendar calendar,
            Interpolator interpolator)
            : base(dates[0], calendar, dayCounter)
        {
            dates_ = dates;
            times_ = new List<double>();
            data_ = hazardRates;
            if (interpolator == null)
            {
                interpolator_ = FastActivator<Interpolator>.Create();
            }
            else
            {
                interpolator_ = interpolator;
            }

            initialize();
        }

        public InterpolatedHazardRateCurve(List<Date> dates, List<double> hazardRates, DayCounter dayCounter, Interpolator interpolator)
            : base(dates[0], null, dayCounter)
        {
            dates_ = dates;
            if (interpolator == null)
            {
                interpolator_ = FastActivator<Interpolator>.Create();
            }
            else
            {
                interpolator_ = interpolator;
            }

            initialize();
        }

        protected InterpolatedHazardRateCurve(DayCounter dc,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dc, jumps, jumpDates)
        {
        }

        protected InterpolatedHazardRateCurve(Date referenceDate, DayCounter dc,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(referenceDate, null, dc, jumps, jumpDates)
        {
        }

        protected InterpolatedHazardRateCurve(int settlementDays, Calendar cal, DayCounter dc,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(settlementDays, cal, dc, jumps, jumpDates)
        {
        }

        public List<double> hazardRates() => data_;

        // DefaultProbabilityTermStructure implementation
        protected override double hazardRateImpl(double t)
        {
            if (t <= times_.Last())
            {
                return interpolation_.value(t, true);
            }

            // flat hazard rate extrapolation
            return data_.Last();
        }

        protected override double survivalProbabilityImpl(double t)
        {
            if (t.IsEqual(0.0))
            {
                return 1.0;
            }

            double integral;
            if (t <= times_.Last())
            {
                integral = interpolation_.primitive(t, true);
            }
            else
            {
                // flat hazard rate extrapolation
                integral = interpolation_.primitive(times_.Last(), true)
                           + data_.Last() * (t - times_.Last());
            }

            return System.Math.Exp(-integral);
        }

        private void initialize()
        {
            Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints, () => "not enough input dates given");
            Utils.QL_REQUIRE(data_.Count == dates_.Count, () => "dates/data count mismatch");

            times_.Add(0.0);
            for (var i = 1; i < dates_.Count; ++i)
            {
                Utils.QL_REQUIRE(dates_[i] > dates_[i - 1], () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
                times_.Add(dayCounter().yearFraction(dates_[0], dates_[i]));
                Utils.QL_REQUIRE(!Utils.close(times_[i], times_[i - 1]), () => "two dates correspond to the same time " +
                                                                               "under this curve's day count convention");
                Utils.QL_REQUIRE(data_[i] >= 0.0, () => "negative hazard rate");
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

        public List<double> discounts() => data_;

        public List<double> data() => discounts();

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
