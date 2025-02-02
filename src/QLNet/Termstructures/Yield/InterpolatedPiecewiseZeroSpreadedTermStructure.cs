﻿/*
 Copyright (C) 2015 Francois Botha

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
    public class InterpolatedPiecewiseZeroSpreadedTermStructure<Interpolator> : ZeroYieldStructure
        where Interpolator : class, IInterpolationFactory, new()
    {
        protected Compounding compounding_;
        protected List<Date> dates_;
        protected DayCounter dc_;
        protected Interpolator factory_;
        protected Frequency frequency_;
        protected Interpolation interpolator_;
        protected Handle<YieldTermStructure> originalCurve_;
        protected List<Handle<Quote>> spreads_;
        protected List<double> spreadValues_;
        protected List<double> times_;

        public InterpolatedPiecewiseZeroSpreadedTermStructure(Handle<YieldTermStructure> h,
            List<Handle<Quote>> spreads,
            List<Date> dates,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.NoFrequency,
            DayCounter dc = default,
            Interpolator factory = default)
        {
            originalCurve_ = h;
            spreads_ = spreads;
            dates_ = dates;
            times_ = new InitializedList<double>(dates.Count);
            spreadValues_ = new InitializedList<double>(dates.Count);
            compounding_ = compounding;
            frequency_ = frequency;
            dc_ = dc ?? new DayCounter();
            factory_ = factory ?? FastActivator<Interpolator>.Create();

            QLNet.Utils.QL_REQUIRE(!spreads_.empty(), () => "no spreads given");
            QLNet.Utils.QL_REQUIRE(spreads_.Count == dates_.Count, () => "spread and date vector have different sizes");

            originalCurve_.registerWith(update);

            for (var i = 0; i < spreads_.Count; i++)
            {
                spreads_[i].registerWith(update);
            }

            if (!originalCurve_.empty())
            {
                updateInterpolation();
            }
        }

        public override Calendar calendar() => originalCurve_.link.calendar();

        public override DayCounter dayCounter() => originalCurve_.link.dayCounter();

        public override Date maxDate() => originalCurve_.link.maxDate() < dates_.Last() ? originalCurve_.link.maxDate() : dates_.Last();

        public override Date referenceDate() => originalCurve_.link.referenceDate();

        public override int settlementDays() => originalCurve_.link.settlementDays();

        protected double calcSpread(double t)
        {
            if (t <= times_.First())
            {
                return spreads_.First().link.value();
            }

            if (t >= times_.Last())
            {
                return spreads_.Last().link.value();
            }

            return interpolator_.value(t, true);
        }

        protected new void update()
        {
            if (!originalCurve_.empty())
            {
                updateInterpolation();
                base.update();
            }
            else
            {
                /* The implementation inherited from YieldTermStructure
                   asks for our reference date, which we don't have since
                   the original curve is still not set. Therefore, we skip
                   over that and just call the base-class behavior. */
                base.update();
            }
        }

        protected void updateInterpolation()
        {
            for (var i = 0; i < dates_.Count; i++)
            {
                times_[i] = timeFromReference(dates_[i]);
                spreadValues_[i] = spreads_[i].link.value();
            }

            interpolator_ = factory_.interpolate(times_, times_.Count, spreadValues_);
        }

        protected override double zeroYieldImpl(double t)
        {
            var spread = calcSpread(t);
            var zeroRate = originalCurve_.link.zeroRate(t, compounding_, frequency_, true);
            var spreadedRate = new InterestRate(zeroRate.value() + spread,
                zeroRate.dayCounter(),
                zeroRate.compounding(),
                zeroRate.frequency());
            return spreadedRate.equivalentRate(Compounding.Continuous, Frequency.NoFrequency, t).value();
        }
    }
}
