﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Optionlet
{
    [PublicAPI]
    public class StrippedOptionletAdapter : OptionletVolatilityStructure
    {
        private int nInterpolations_;
        private StrippedOptionletBase optionletStripper_;
        private List<Interpolation> strikeInterpolations_;

        /*! Adapter class for turning a StrippedOptionletBase object into an
         OptionletVolatilityStructure.
        */
        public StrippedOptionletAdapter(StrippedOptionletBase s)
            : base(s.settlementDays(), s.calendar(), s.businessDayConvention(), s.dayCounter())
        {
            optionletStripper_ = s;
            nInterpolations_ = s.optionletMaturities();
            strikeInterpolations_ = new List<Interpolation>(nInterpolations_);

            optionletStripper_.registerWith(update);
        }

        public override double displacement() => optionletStripper_.displacement();

        // TermStructure interface

        public override Date maxDate() => optionletStripper_.optionletFixingDates().Last();

        public override double maxStrike() => optionletStripper_.optionletStrikes(0).Last();

        // VolatilityTermStructure interface

        public override double minStrike() => optionletStripper_.optionletStrikes(0).First();

        // LazyObject interface

        public override void update()
        {
            base.update();
        }

        public override VolatilityType volatilityType() => optionletStripper_.volatilityType();

        protected override void performCalculations()
        {
            for (var i = 0; i < nInterpolations_; ++i)
            {
                var optionletStrikes = new List<double>(optionletStripper_.optionletStrikes(i));
                var optionletVolatilities = new List<double>(optionletStripper_.optionletVolatilities(i));
                strikeInterpolations_.Add(new LinearInterpolation(optionletStrikes, optionletStrikes.Count, optionletVolatilities));
            }
        }

        // OptionletVolatilityStructure interface

        protected override SmileSection smileSectionImpl(double t)
        {
            var optionletStrikes = new List<double>(optionletStripper_.optionletStrikes(0)); // strikes are the same for all times ?!
            var stddevs = new List<double>();
            for (var i = 0; i < optionletStrikes.Count; i++)
            {
                stddevs.Add(volatilityImpl(t, optionletStrikes[i]) * System.Math.Sqrt(t));
            }

            // Extrapolation may be a problem with splines, but since minStrike() and maxStrike() are set, we assume that no one will use stddevs for strikes outside these strikes
            var bc = optionletStrikes.Count >= 4 ? CubicInterpolation.BoundaryCondition.Lagrange : CubicInterpolation.BoundaryCondition.SecondDerivative;
            return new InterpolatedSmileSection<Cubic>(t, optionletStrikes, stddevs, 0,
                new Cubic(CubicInterpolation.DerivativeApprox.Spline, false, bc, 0.0, bc, 0.0));
        }

        protected override double volatilityImpl(double length, double strike)
        {
            calculate();

            List<double> vol = new InitializedList<double>(nInterpolations_);
            for (var i = 0; i < nInterpolations_; ++i)
            {
                vol[i] = strikeInterpolations_[i].value(strike, true);
            }

            var optionletTimes = new List<double>(optionletStripper_.optionletFixingTimes());
            var timeInterpolator = new LinearInterpolation(optionletTimes, optionletTimes.Count, vol);
            return timeInterpolator.value(length, true);
        }
    }
}
