/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;
using QLNet.Instruments;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Extensions;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;

namespace QLNet.Tests
{
    public static class Utilities
    {
        public static YieldTermStructure flatRate(Date today, double forward, DayCounter dc) => new FlatForward(today, new SimpleQuote(forward), dc);

        public static YieldTermStructure flatRate(Date today, Quote forward, DayCounter dc) => new FlatForward(today, forward, dc);

        //philippe2009_17
        public static YieldTermStructure flatRate(double forward, DayCounter dc) => flatRate(new SimpleQuote(forward), dc);

        public static YieldTermStructure flatRate(Quote forward, DayCounter dc) => new FlatForward(0, new NullCalendar(), forward, dc);

        public static BlackVolTermStructure flatVol(Date today, double vol, DayCounter dc) => flatVol(today, new SimpleQuote(vol), dc);

        public static BlackVolTermStructure flatVol(Date today, Quote vol, DayCounter dc) => new BlackConstantVol(today, new NullCalendar(), new Handle<Quote>(vol), dc);

        //philippe2009_17
        public static BlackVolTermStructure flatVol(Quote vol, DayCounter dc) => new BlackConstantVol(0, new NullCalendar(), new Handle<Quote>(vol), dc);

        public static BlackVolTermStructure flatVol(double vol, DayCounter dc) => flatVol(new SimpleQuote(vol), dc);

        public static double norm(Vector v, int size, double h)
        {
            // squared values
            List<double> f2 = new InitializedList<double>(size);

            for (var i = 0; i < v.Count; i++)
            {
                f2[i] = v[i] * v[i];
            }

            // numeric integral of f^2
            var I = h * (f2.Sum() - 0.5 * f2.First() - 0.5 * f2.Last());
            return System.Math.Sqrt(I);
        }

        public static double relativeError(double x1, double x2, double reference)
        {
            if (reference.IsNotEqual(0.0))
            {
                return System.Math.Abs(x1 - x2) / reference;
            }
            else
                // fall back to absolute error
            {
                return System.Math.Abs(x1 - x2);
            }
        }

        public static string exerciseTypeToString(Exercise h)
        {
            object hd = null;

            hd = h as EuropeanExercise;
            if (hd != null)
            {
                return "European";
            }

            hd = h as AmericanExercise;
            if (hd != null)
            {
                return "American";
            }

            hd = h as BermudanExercise;
            if (hd != null)
            {
                return "Bermudan";
            }

            QLNet.Utils.QL_FAIL("unknown exercise ExerciseType");
            return string.Empty;
        }

        public static string payoffTypeToString(Payoff h)
        {
            object hd = null;
            hd = h as PlainVanillaPayoff;
            if (hd != null)
            {
                return "plain-vanilla";
            }

            hd = h as CashOrNothingPayoff;
            if (hd != null)
            {
                return "cash-or-nothing";
            }

            hd = h as AssetOrNothingPayoff;
            if (hd != null)
            {
                return "asset-or-nothing";
            }

            hd = h as SuperSharePayoff;
            if (hd != null)
            {
                return "super-share";
            }

            hd = h as SuperFundPayoff;
            if (hd != null)
            {
                return "super-fund";
            }

            hd = h as PercentageStrikePayoff;
            if (hd != null)
            {
                return "percentage-strike";
            }

            hd = h as GapPayoff;
            if (hd != null)
            {
                return "gap";
            }

            hd = h as FloatingTypePayoff;
            if (hd != null)
            {
                return "floating-ExerciseType";
            }

            QLNet.Utils.QL_FAIL("unknown payoff ExerciseType");
            return string.Empty;
        }
    }

    // this cleans up index-fixing histories when disposed
}
