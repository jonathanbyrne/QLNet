﻿//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Termstructures.Credit
{
    /// <summary>
    /// DefaultProbabilityTermStructure based on interpolation of survival probabilities
    /// </summary>
    /// <typeparam name="Interpolator"></typeparam>
    public class InterpolatedSurvivalProbabilityCurve<Interpolator> : SurvivalProbabilityStructure,
      InterpolatedCurve where Interpolator : IInterpolationFactory, new()
    {
        public InterpolatedSurvivalProbabilityCurve(List<Date> dates,
                                                    List<double> probabilities,
                                                    DayCounter dayCounter,
                                                    Calendar calendar = null,
                                                    List<Handle<Quote>> jumps = null,
                                                    List<Date> jumpDates = null,
                                                    Interpolator interpolator = default)
           : base(dates[0], calendar, dayCounter, jumps, jumpDates)
        {
            dates_ = dates;

            Utils.QL_REQUIRE(dates_.Count >= interpolator.requiredPoints, () => "not enough input dates given");
            Utils.QL_REQUIRE(data_.Count == dates_.Count, () => "dates/data count mismatch");
            Utils.QL_REQUIRE(data_[0].IsEqual(1.0), () => "the first probability must be == 1.0 to flag the corresponding date as reference date");

            times_ = new InitializedList<double>(dates_.Count);
            times_[0] = 0.0;
            for (int i = 1; i < dates_.Count; ++i)
            {
                Utils.QL_REQUIRE(dates_[i] > dates_[i - 1], () =>
                                 "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
                times_[i] = dayCounter.yearFraction(dates_[0], dates_[i]);
                Utils.QL_REQUIRE(!Utils.close(times_[i], times_[i - 1]), () =>
                                 "two dates correspond to the same time under this curve's day count convention");
                Utils.QL_REQUIRE(data_[i] > 0.0, () => "negative probability");
                Utils.QL_REQUIRE(data_[i] <= data_[i - 1], () =>
                                 "negative hazard rate implied by the survival probability " +
                                 data_[i] + " at " + dates_[i] +
                                 " (t=" + times_[i] + ") after the survival probability " +
                                 data_[i - 1] + " at " + dates_[i - 1] +
                                 " (t=" + times_[i - 1] + ")");
            }

            interpolation_ = interpolator_.interpolate(times_,
                                                                 times_.Count,
                                                                 data_);
            interpolation_.update();

        }
        /// <summary>
        /// TermStructure interface
        /// </summary>
        /// <returns></returns>
        public override Date maxDate() { return dates_.Last(); }

        // other inspectors
        public List<double> times() { return times_; }
        public List<Date> dates() { return dates_; }
        public List<double> data() { return data_; }
        public List<double> survivalProbabilities() { return data_; }
        public Dictionary<Date, double> nodes()
        {
            Dictionary<Date, double> results = new Dictionary<Date, double>();
            dates_.ForEach((i, x) => results.Add(x, data_[i]));
            return results;
        }

        protected InterpolatedSurvivalProbabilityCurve(DayCounter dc,
                                                       List<Handle<Quote>> jumps = null,
                                                       List<Date> jumpDates = null,
                                                       Interpolator interpolator = default)
           : base(dc, jumps, jumpDates) { }

        protected InterpolatedSurvivalProbabilityCurve(Date referenceDate,
                                                       DayCounter dc,
                                                       List<Handle<Quote>> jumps = null,
                                                       List<Date> jumpDates = null,
                                                       Interpolator interpolator = default)
           : base(referenceDate, new Calendar(), dc, jumps, jumpDates) { }

        protected InterpolatedSurvivalProbabilityCurve(int settlementDays,
                                                       Calendar cal,
                                                       DayCounter dc,
                                                       List<Handle<Quote>> jumps = null,
                                                       List<Date> jumpDates = null,
                                                       Interpolator interpolator = default)
           : base(settlementDays, cal, dc, jumps, jumpDates) { }

        /// <summary>
        /// DefaultProbabilityTermStructure implementation
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected override double survivalProbabilityImpl(double t)
        {
            if (t <= times_.Last())
                return interpolation_.value(t, true);

            // flat hazard rate extrapolation
            double tMax = times_.Last();
            double sMax = data_.Last();
            double hazardMax = -interpolation_.derivative(tMax) / sMax;
            return sMax * System.Math.Exp(-hazardMax * (t - tMax));
        }

        protected override double defaultDensityImpl(double t)
        {
            if (t <= times_.Last())
                return -interpolation_.derivative(t, true);

            // flat hazard rate extrapolation
            double tMax = times_.Last();
            double sMax = data_.Last();
            double hazardMax = -interpolation_.derivative(tMax) / sMax;
            return sMax * hazardMax * System.Math.Exp(-hazardMax * (t - tMax));
        }


        #region InterpolatedCurve

        public List<double> times_ { get; set; }

        public List<Date> dates_ { get; set; }
        public Date maxDate_ { get; set; }

        public List<double> data_ { get; set; }
        public List<double> discounts() { return data_; }

        public Interpolation interpolation_ { get; set; }
        public IInterpolationFactory interpolator_ { get; set; }

        public void setupInterpolation()
        {
            interpolation_ = interpolator_.interpolate(times_, times_.Count, data_);
        }

        public object Clone()
        {
            InterpolatedCurve copy = MemberwiseClone() as InterpolatedCurve;
            copy.times_ = new List<double>(times_);
            copy.data_ = new List<double>(data_);
            copy.interpolator_ = interpolator_;
            copy.setupInterpolation();
            return copy;
        }
        #endregion
    }
}
