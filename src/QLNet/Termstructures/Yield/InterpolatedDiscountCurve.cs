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
    //! Term structure based on interpolation of discount factors
    /*! \ingroup yieldtermstructures */
    [PublicAPI]
    public class InterpolatedDiscountCurve<Interpolator> : YieldTermStructure, InterpolatedCurve
        where Interpolator : class, IInterpolationFactory, new()
    {
        public InterpolatedDiscountCurve(DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedDiscountCurve(Date referenceDate,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(referenceDate, null, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedDiscountCurve(int settlementDays,
            Calendar calendar,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(settlementDays, calendar, dayCounter, jumps, jumpDates)
        {
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
        }

        public InterpolatedDiscountCurve(List<Date> dates,
            List<double> discounts,
            DayCounter dayCounter,
            Calendar calendar = null,
            List<Handle<Quote>> jumps = null,
            List<Date> jumpDates = null,
            Interpolator interpolator = default)
            : base(dates[0], calendar, dayCounter, jumps, jumpDates)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = discounts;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
            initialize();
        }

        public InterpolatedDiscountCurve(List<Date> dates,
            List<double> discounts,
            DayCounter dayCounter,
            Calendar calendar,
            Interpolator interpolator)
            : base(dates[0], calendar, dayCounter)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = discounts;
            interpolator_ = interpolator;
            initialize();
        }

        public InterpolatedDiscountCurve(List<Date> dates,
            List<double> discounts,
            DayCounter dayCounter,
            Interpolator interpolator)
            : base(dates[0], null, dayCounter)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = discounts;
            interpolator_ = interpolator;
            initialize();
        }

        public InterpolatedDiscountCurve(List<Date> dates,
            List<double> discounts,
            DayCounter dayCounter,
            List<Handle<Quote>> jumps,
            List<Date> jumpDates,
            Interpolator interpolator = default)
            : base(dates[0], null, dayCounter, jumps, jumpDates)
        {
            times_ = new List<double>();
            dates_ = dates;
            data_ = discounts;
            interpolator_ = interpolator ?? FastActivator<Interpolator>.Create();
            initialize();
        }

        protected override double discountImpl(double t)
        {
            if (t <= times_.Last())
            {
                return interpolation_.value(t, true);
            }

            // flat fwd extrapolation
            var tMax = times_.Last();
            var dMax = data_.Last();
            var instFwdMax = -interpolation_.derivative(tMax) / dMax;
            return dMax * System.Math.Exp(-instFwdMax * (t - tMax));
        }

        private void initialize()
        {
            QLNet.Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints,
                () => "not enough input dates given");
            QLNet.Utils.QL_REQUIRE(data_.Count == dates_.Count,
                () => "dates/data count mismatch");
            QLNet.Utils.QL_REQUIRE(data_[0].IsEqual(1.0), () => "the first discount must be == 1.0 " +
                                                          "to flag the corrsponding date as settlement date");

            times_ = new InitializedList<double>(dates_.Count - 1);
            times_.Add(0.0);
            for (var i = 1; i < dates_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(dates_[i] > dates_[i - 1],
                    () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
                times_[i] = dayCounter().yearFraction(dates_[0], dates_[i]);
                QLNet.Utils.QL_REQUIRE(!Math.Utils.close(times_[i], times_[i - 1]),
                    () => "two dates correspond to the same time " +
                          "under this curve's day count convention");
                QLNet.Utils.QL_REQUIRE(data_[i] > 0.0, () => "negative discount");

#if !QL_NEGATIVE_RATES
            QLNet.Utils.QL_REQUIRE(this.data_[i] <= this.data_[i - 1],
                             () => "negative forward rate implied by the discount " +
                             this.data_[i] + " at " + dates_[i] +
                             " (t=" + this.times_[i] + ") after the discount " +
                             this.data_[i - 1] + " at " + dates_[i - 1] +
                             " (t=" + this.times_[i - 1] + ")");
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
