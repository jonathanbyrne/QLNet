/*
 Copyright (C) 2018 Francois Botha (igitur@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class CompositeZeroYieldStructure : ZeroYieldStructure
    {
        private readonly Compounding comp_;
        private readonly Handle<YieldTermStructure> curve1_;
        private readonly Handle<YieldTermStructure> curve2_;
        private readonly Func<double, double, double> f_;
        private readonly Frequency freq_;

        public CompositeZeroYieldStructure(Handle<YieldTermStructure> h1,
            Handle<YieldTermStructure> h2,
            Func<double, double, double> f,
            Compounding comp = Compounding.Continuous,
            Frequency freq = Frequency.NoFrequency)
        {
            curve1_ = h1;
            curve2_ = h2;
            f_ = f;
            comp_ = comp;
            freq_ = freq;

            if (!curve1_.empty() && !curve2_.empty())
            {
                enableExtrapolation(curve1_.link.allowsExtrapolation() && curve2_.link.allowsExtrapolation());
            }

            curve1_.registerWith(update);
            curve2_.registerWith(update);
        }

        public override Calendar calendar() => curve1_.link.calendar();

        public override DayCounter dayCounter() => curve1_.link.dayCounter();

        public override Date maxDate() => curve1_.link.maxDate();

        public override double maxTime() => curve1_.link.maxTime();

        public override Date referenceDate() => curve1_.link.referenceDate();

        public override int settlementDays() => curve1_.link.settlementDays();

        public override void update()
        {
            if (!curve1_.empty() && !curve2_.empty())
            {
                base.update();
                enableExtrapolation(curve1_.link.allowsExtrapolation() && curve2_.link.allowsExtrapolation());
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

        protected override double zeroYieldImpl(double t)
        {
            var zeroRate1 = curve1_.link.zeroRate(t, comp_, freq_, true).rate();
            var zeroRate2 = curve2_.link.zeroRate(t, comp_, freq_, true).rate();

            var compositeRate = new InterestRate(f_(zeroRate1, zeroRate2), dayCounter(), comp_, freq_);
            return compositeRate.equivalentRate(Compounding.Continuous, Frequency.NoFrequency, t).value();
        }
    }
}
