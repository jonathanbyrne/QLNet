/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)

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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Termstructures.Yield;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [PublicAPI]
    public class PiecewiseZeroInflationCurve : ZeroInflationTermStructure, Curve<ZeroInflationTermStructure>
    {
        public PiecewiseZeroInflationCurve(DayCounter dayCounter, double baseZeroRate, Period observationLag, Frequency frequency,
            bool indexIsInterpolated, Handle<YieldTermStructure> yTS)
            : base(dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS)
        {
        }

        public PiecewiseZeroInflationCurve(Date referenceDate, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
            Period observationLag, Frequency frequency, bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS)
            : base(referenceDate, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS)
        {
        }

        public PiecewiseZeroInflationCurve(int settlementDays, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
            Period observationLag, Frequency frequency, bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS)
            : base(settlementDays, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS)
        {
        }

        public PiecewiseZeroInflationCurve()
        {
        }

        // these are dummy methods (for the sake of ITraits and should not be called directly
        public double discountImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        public List<double> rates() => data_;

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();

        protected override double zeroRateImpl(double t) => interpolation_.value(t, true);

        #region InflationTraits

        public Date initialDate(ZeroInflationTermStructure c) => traits_.initialDate(c);

        public double initialValue(ZeroInflationTermStructure c) => traits_.initialValue(c);

        public double guess(int i, InterpolatedCurve c, bool validData, int first) => traits_.guess(i, c, validData, first);

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int first) => traits_.minValueAfter(i, c, validData, first);

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int first) => traits_.maxValueAfter(i, c, validData, first);

        public void updateGuess(List<double> data, double discount, int i)
        {
            traits_.updateGuess(data, discount, i);
        }

        public int maxIterations() => traits_.maxIterations();

        #endregion

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

        #region new fields: Curve

        public double initialValue() => _traits_.initialValue(this);

        public Date initialDate() => _traits_.initialDate(this);

        public void registerWith(BootstrapHelper<ZeroInflationTermStructure> helper)
        {
            helper.registerWith(update);
        }

        //public new bool moving_
        public new bool moving_
        {
            get => base.moving_;
            set => base.moving_ = value;
        }

        public void setTermStructure(BootstrapHelper<ZeroInflationTermStructure> helper)
        {
            helper.setTermStructure(this);
        }

        protected ITraits<ZeroInflationTermStructure> _traits_; //todo define with the trait for yield curve

        public ITraits<ZeroInflationTermStructure> traits_ => _traits_;

        protected List<BootstrapHelper<ZeroInflationTermStructure>> _instruments_ = new List<BootstrapHelper<ZeroInflationTermStructure>>();

        public List<BootstrapHelper<ZeroInflationTermStructure>> instruments_
        {
            get
            {
                //todo edem
                var instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
                _instruments_.ForEach((i, x) => instruments.Add(x));
                return instruments;
            }
        }

        protected IBootStrap<PiecewiseZeroInflationCurve> bootstrap_;
        protected double _accuracy_;

        public double accuracy_
        {
            get => _accuracy_;
            set => _accuracy_ = value;
        }

        public override Date baseDate() =>
            // if indexIsInterpolated we fixed the dates in the constructor
            dates_.First();

        #endregion
    }

    [PublicAPI]
    public class PiecewiseZeroInflationCurve<Interpolator, Bootstrap, Traits> : PiecewiseZeroInflationCurve
        where Traits : ITraits<ZeroInflationTermStructure>, new()
        where Interpolator : IInterpolationFactory, new()
        where Bootstrap : IBootStrap<PiecewiseZeroInflationCurve>, new()
    {
        public PiecewiseZeroInflationCurve(Date referenceDate,
            Calendar calendar,
            DayCounter dayCounter,
            Period lag,
            Frequency frequency,
            bool indexIsInterpolated,
            double baseZeroRate,
            Handle<YieldTermStructure> nominalTS,
            List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
            double accuracy = 1.0e-12,
            Interpolator i = default,
            Bootstrap bootstrap = default)
            : base(referenceDate, calendar, dayCounter, baseZeroRate, lag, frequency, indexIsInterpolated, nominalTS)
        {
            _instruments_ = instruments;
            // ensure helpers are sorted
            _instruments_.Sort((x, y) => x.pillarDate().CompareTo(y.pillarDate()));

            accuracy_ = accuracy;
            if (bootstrap == null)
            {
                bootstrap_ = FastActivator<Bootstrap>.Create();
            }
            else
            {
                bootstrap_ = bootstrap;
            }

            if (i == null)
            {
                interpolator_ = FastActivator<Interpolator>.Create();
            }
            else
            {
                interpolator_ = i;
            }

            _traits_ = FastActivator<Traits>.Create();
            bootstrap_.setup(this);
        }

        // Inflation interface
        public override Date baseDate()
        {
            calculate();
            return base.baseDate();
        }

        public override List<double> data()
        {
            calculate();
            return rates();
        }

        public override List<Date> dates()
        {
            calculate();
            return base.dates();
        }

        public override Date maxDate()
        {
            calculate();
            return base.maxDate();
        }

        public override Dictionary<Date, double> nodes()
        {
            calculate();
            return base.nodes();
        }

        // Inspectors
        public override List<double> times()
        {
            calculate();
            return base.times();
        }

        // methods
        protected override void performCalculations()
        {
            bootstrap_.calculate();
        }
    }

    // Allows for optional 3rd generic parameter defaulted to IterativeBootstrap
    [PublicAPI]
    public class PiecewiseZeroInflationCurve<Interpolator> : PiecewiseZeroInflationCurve<Interpolator, IterativeBootstrapForInflation, ZeroInflationTraits>
        where Interpolator : IInterpolationFactory, new()
    {
        public PiecewiseZeroInflationCurve(Date referenceDate,
            Calendar calendar,
            DayCounter dayCounter,
            Period lag,
            Frequency frequency,
            bool indexIsInterpolated,
            double baseZeroRate,
            Handle<YieldTermStructure> nominalTS,
            List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
            double accuracy = 1.0e-12,
            Interpolator i = default)
            : base(referenceDate, calendar, dayCounter, lag, frequency, indexIsInterpolated, baseZeroRate, nominalTS,
                instruments, accuracy, i)
        {
        }
    }
}
