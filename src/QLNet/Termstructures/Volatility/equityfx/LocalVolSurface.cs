﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.equityfx
{
    //! Local volatility surface derived from a Black vol surface
    /*! For details about this implementation refer to
        "Stochastic Volatility and Local Volatility," in
        "Case Studies and Financial Modelling Course Notes," by
        Jim Gatheral, Fall Term, 2003

        see www.math.nyu.edu/fellows_fin_math/gatheral/Lecture1_Fall02.pdf

        \bug this class is untested, probably unreliable.
    */
    [PublicAPI]
    public class LocalVolSurface : LocalVolTermStructure
    {
        private Handle<BlackVolTermStructure> blackTS_;
        private Handle<YieldTermStructure> riskFreeTS_, dividendTS_;
        private Handle<Quote> underlying_;

        public LocalVolSurface(Handle<BlackVolTermStructure> blackTS, Handle<YieldTermStructure> riskFreeTS,
            Handle<YieldTermStructure> dividendTS, Handle<Quote> underlying)
            : base(blackTS.link.businessDayConvention(), blackTS.link.dayCounter())
        {
            blackTS_ = blackTS;
            riskFreeTS_ = riskFreeTS;
            dividendTS_ = dividendTS;
            underlying_ = underlying;

            blackTS_.registerWith(update);
            riskFreeTS_.registerWith(update);
            dividendTS_.registerWith(update);
            underlying_.registerWith(update);
        }

        public LocalVolSurface(Handle<BlackVolTermStructure> blackTS, Handle<YieldTermStructure> riskFreeTS,
            Handle<YieldTermStructure> dividendTS, double underlying)
            : base(blackTS.link.businessDayConvention(), blackTS.link.dayCounter())
        {
            blackTS_ = blackTS;
            riskFreeTS_ = riskFreeTS;
            dividendTS_ = dividendTS;
            underlying_ = new Handle<Quote>(new SimpleQuote(underlying));

            blackTS_.registerWith(update);
            riskFreeTS_.registerWith(update);
            dividendTS_.registerWith(update);
            underlying_.registerWith(update);
        }

        public override DayCounter dayCounter() => blackTS_.link.dayCounter();

        public override Date maxDate() => blackTS_.link.maxDate();

        public override double maxStrike() => blackTS_.link.maxStrike();

        // VolatilityTermStructure interface
        public override double minStrike() => blackTS_.link.minStrike();

        // TermStructure interface
        public override Date referenceDate() => blackTS_.link.referenceDate();

        protected override double localVolImpl(double t, double underlyingLevel)
        {
            var dr = riskFreeTS_.currentLink().discount(t, true);
            var dq = dividendTS_.currentLink().discount(t, true);
            var forwardValue = underlying_.currentLink().value() * dq / dr;

            // strike derivatives
            double strike, y, dy, strikep, strikem;
            double w, wp, wm, dwdy, d2wdy2;
            strike = underlyingLevel;
            y = System.Math.Log(strike / forwardValue);
            dy = System.Math.Abs(y) > 0.001 ? y * 0.0001 : 0.000001;
            strikep = strike * System.Math.Exp(dy);
            strikem = strike / System.Math.Exp(dy);
            w = blackTS_.link.blackVariance(t, strike, true);
            wp = blackTS_.link.blackVariance(t, strikep, true);
            wm = blackTS_.link.blackVariance(t, strikem, true);
            dwdy = (wp - wm) / (2.0 * dy);
            d2wdy2 = (wp - 2.0 * w + wm) / (dy * dy);

            // time derivative
            double dt, wpt, wmt, dwdt;
            if (t.IsEqual(0.0))
            {
                dt = 0.0001;
                var drpt = riskFreeTS_.currentLink().discount(t + dt, true);
                var dqpt = dividendTS_.currentLink().discount(t + dt, true);
                var strikept = strike * dr * dqpt / (drpt * dq);

                wpt = blackTS_.link.blackVariance(t + dt, strikept, true);

                QLNet.Utils.QL_REQUIRE(wpt >= w, () =>
                    "decreasing variance at strike " + strike + " between time " + t + " and time " + (t + dt));
                dwdt = (wpt - w) / dt;
            }
            else
            {
                dt = System.Math.Min(0.0001, t / 2.0);
                var drpt = riskFreeTS_.currentLink().discount(t + dt, true);
                var drmt = riskFreeTS_.currentLink().discount(t - dt, true);
                var dqpt = dividendTS_.currentLink().discount(t + dt, true);
                var dqmt = dividendTS_.currentLink().discount(t - dt, true);

                var strikept = strike * dr * dqpt / (drpt * dq);
                var strikemt = strike * dr * dqmt / (drmt * dq);

                wpt = blackTS_.link.blackVariance(t + dt, strikept, true);
                wmt = blackTS_.link.blackVariance(t - dt, strikemt, true);
                QLNet.Utils.QL_REQUIRE(wpt >= w, () =>
                    "decreasing variance at strike " + strike + " between time " + t + " and time " + (t + dt));
                QLNet.Utils.QL_REQUIRE(w >= wmt, () =>
                    "decreasing variance at strike " + strike + " between time " + (t - dt) + " and time " + t);
                dwdt = (wpt - wmt) / (2.0 * dt);
            }

            if (dwdy.IsEqual(0.0) && d2wdy2.IsEqual(0.0)) // avoid /w where w might be 0.0
            {
                return System.Math.Sqrt(dwdt);
            }

            var den1 = 1.0 - y / w * dwdy;
            var den2 = 0.25 * (-0.25 - 1.0 / w + y * y / w / w) * dwdy * dwdy;
            var den3 = 0.5 * d2wdy2;
            var den = den1 + den2 + den3;
            var result = dwdt / den;
            QLNet.Utils.QL_REQUIRE(result >= 0.0, () =>
                "negative local vol^2 at strike " + strike + " and time " + t + "; the black vol surface is not smooth enough");
            return System.Math.Sqrt(result);
        }
    }
}
