﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)

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
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    // this is an abstract class to give access to all properties and methods of PiecewiseYieldCurve and avoiding generics
    [PublicAPI]
    public class PiecewiseYieldCurve : YieldTermStructure, Curve<YieldTermStructure>
    {
        // two constructors to forward down the ctor chain
        public PiecewiseYieldCurve(Date referenceDate, Calendar cal, DayCounter dc,
            List<Handle<Quote>> jumps = null, List<Date> jumpDates = null) : base(referenceDate, cal, dc, jumps, jumpDates)
        {
        }

        public PiecewiseYieldCurve(int settlementDays, Calendar cal, DayCounter dc,
            List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
            : base(settlementDays, cal, dc, jumps, jumpDates)
        {
        }

        public PiecewiseYieldCurve()
        {
        }

        #region new fields: Curve

        public double initialValue() => _traits_.initialValue(this);

        public Date initialDate() => _traits_.initialDate(this);

        public void registerWith(BootstrapHelper<YieldTermStructure> helper)
        {
            helper.registerWith(update);
        }

        public new bool moving_
        {
            get => base.moving_;
            set => base.moving_ = value;
        }

        public void setTermStructure(BootstrapHelper<YieldTermStructure> helper)
        {
            helper.setTermStructure(this);
        }

        protected ITraits<YieldTermStructure> _traits_; //todo define with the trait for yield curve

        public ITraits<YieldTermStructure> traits_ => _traits_;

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int first) => traits_.minValueAfter(i, c, validData, first);

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int first) => traits_.maxValueAfter(i, c, validData, first);

        public double guess(int i, InterpolatedCurve c, bool validData, int first) => traits_.guess(i, c, validData, first);

        #endregion

        #region InterpolatedCurve

        public List<double> times_ { get; set; }

        public List<double> times()
        {
            calculate();
            return times_;
        }

        public List<Date> dates_ { get; set; }

        public List<Date> dates()
        {
            calculate();
            return dates_;
        }

        // here we do not refer to the base curve as in QL because our base curve is YieldTermStructure and not Traits::base_curve
        public Date maxDate_ { get; set; }

        public override Date maxDate()
        {
            calculate();
            if (maxDate_ != null)
            {
                return maxDate_;
            }

            return dates_.Last();
        }

        public List<double> data_ { get; set; }

        public List<double> data()
        {
            calculate();
            return data_;
        }

        public Interpolation interpolation_ { get; set; }

        public IInterpolationFactory interpolator_ { get; set; }

        public Dictionary<Date, double> nodes()
        {
            calculate();
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

        #region BootstrapTraits

        public Date initialDate(YieldTermStructure c) => traits_.initialDate(c);

        public double initialValue(YieldTermStructure c) => traits_.initialValue(c);

        public void updateGuess(List<double> data, double discount, int i)
        {
            traits_.updateGuess(data, discount, i);
        }

        public int maxIterations() => traits_.maxIterations();

        protected override double discountImpl(double t)
        {
            calculate();
            return traits_.discountImpl(interpolation_, t);
        }

        protected double zeroYieldImpl(double t) => traits_.zeroYieldImpl(interpolation_, t);

        protected double forwardImpl(double t) => traits_.forwardImpl(interpolation_, t);

        // these are dummy methods (for the sake of ITraits and should not be called directly
        public double discountImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        #endregion

        #region Properties

        protected double _accuracy_;

        public double accuracy_
        {
            get => _accuracy_;
            set => _accuracy_ = value;
        }

        protected List<RateHelper> _instruments_ = new List<RateHelper>();

        public List<BootstrapHelper<YieldTermStructure>> instruments_
        {
            get
            {
                //todo edem
                var instruments = new List<BootstrapHelper<YieldTermStructure>>();
                _instruments_.ForEach((i, x) => instruments.Add(x));
                return instruments;
            }
        }

        protected IBootStrap<PiecewiseYieldCurve> bootstrap_;

        #endregion
    }

    [PublicAPI]
    public class PiecewiseYieldCurve<Traits, Interpolator, BootStrap> : PiecewiseYieldCurve
        where Traits : ITraits<YieldTermStructure>, new()
        where Interpolator : IInterpolationFactory, new()
        where BootStrap : IBootStrap<PiecewiseYieldCurve>, new()
    {
        // observer interface
        public override void update()
        {
            base.update();
            // LazyObject::update();        // we do it in the TermStructure
        }

        protected override void performCalculations()
        {
            // just delegate to the bootstrapper
            bootstrap_.calculate();
        }

        #region Constructors

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments, DayCounter dayCounter)
            : this(referenceDate, instruments, dayCounter, new List<Handle<Quote>>(), new List<Date>(),
                1.0e-12, FastActivator<Interpolator>.Create(), FastActivator<BootStrap>.Create())
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates)
            : this(referenceDate, instruments, dayCounter, jumps, jumpDates, 1.0e-12, FastActivator<Interpolator>.Create(),
                FastActivator<BootStrap>.Create())
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps,
            List<Date> jumpDates, double accuracy)
            : this(referenceDate, instruments, dayCounter, jumps, jumpDates, accuracy, FastActivator<Interpolator>.Create(),
                FastActivator<BootStrap>.Create())
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps,
            List<Date> jumpDates, double accuracy, Interpolator i)
            : this(referenceDate, instruments, dayCounter, jumps, jumpDates, accuracy, i, FastActivator<BootStrap>.Create())
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates,
            double accuracy, Interpolator i, BootStrap bootstrap)
            : base(referenceDate, new Calendar(), dayCounter, jumps, jumpDates)
        {
            _instruments_ = instruments;
            // ensure helpers are sorted
            _instruments_.Sort((x, y) => x.pillarDate().CompareTo(y.pillarDate()));

            accuracy_ = accuracy;
            interpolator_ = i;
            bootstrap_ = bootstrap;
            _traits_ = FastActivator<Traits>.Create();

            bootstrap_.setup(this);
        }

        public PiecewiseYieldCurve(int settlementDays, Calendar calendar, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates, double accuracy)
            : this(settlementDays, calendar, instruments, dayCounter, jumps, jumpDates, accuracy,
                FastActivator<Interpolator>.Create(), FastActivator<BootStrap>.Create())
        {
        }

        public PiecewiseYieldCurve(int settlementDays, Calendar calendar, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates, double accuracy,
            Interpolator i, BootStrap bootstrap)
            : base(settlementDays, calendar, dayCounter, jumps, jumpDates)
        {
            _instruments_ = instruments;
            // ensure helpers are sorted
            _instruments_.Sort((x, y) => x.pillarDate().CompareTo(y.pillarDate()));

            accuracy_ = accuracy;
            interpolator_ = i;
            bootstrap_ = bootstrap;
            _traits_ = FastActivator<Traits>.Create();

            bootstrap_.setup(this);
        }

        #endregion
    }

    // Allows for optional 3rd generic parameter defaulted to IterativeBootstrap
    [PublicAPI]
    public class PiecewiseYieldCurve<Traits, Interpolator> : PiecewiseYieldCurve<Traits, Interpolator, IterativeBootstrapForYield>
        where Traits : ITraits<YieldTermStructure>, new()
        where Interpolator : IInterpolationFactory, new()
    {
        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments, DayCounter dayCounter)
            : base(referenceDate, instruments, dayCounter)
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates)
            : base(referenceDate, instruments, dayCounter, jumps, jumpDates)
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates, double accuracy)
            : base(referenceDate, instruments, dayCounter, jumps, jumpDates, accuracy)
        {
        }

        public PiecewiseYieldCurve(Date referenceDate, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates, double accuracy, Interpolator i)
            : base(referenceDate, instruments, dayCounter, jumps, jumpDates, accuracy, i)
        {
        }

        public PiecewiseYieldCurve(int settlementDays, Calendar calendar, List<RateHelper> instruments,
            DayCounter dayCounter)
            : this(settlementDays, calendar, instruments, dayCounter, new List<Handle<Quote>>(), new List<Date>(), 1.0e-12)
        {
        }

        public PiecewiseYieldCurve(int settlementDays, Calendar calendar, List<RateHelper> instruments,
            DayCounter dayCounter, List<Handle<Quote>> jumps, List<Date> jumpDates, double accuracy)
            : base(settlementDays, calendar, instruments, dayCounter, jumps, jumpDates, accuracy)
        {
        }
    }
}
