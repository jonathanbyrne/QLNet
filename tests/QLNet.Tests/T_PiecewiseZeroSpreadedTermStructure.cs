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

using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using QLNet.Math.Interpolations;
using QLNet.Termstructures.Yield;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.Quotes;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_PiecewiseZeroSpreadedTermStructure
    {
        [JetBrains.Annotations.PublicAPI] public class CommonVars
        {
            // common data
            public Calendar calendar;
            public int settlementDays;
            public DayCounter dayCount;
            public Compounding compounding;
            public YieldTermStructure termStructure;
            public Date today;
            public Date settlementDate;

            // cleanup
            public SavedSettings backup;

            // setup
            public CommonVars()
            {
                // force garbage collection
                // garbage collection in .NET is rather weird and we do need when we run several tests in a row
                GC.Collect();

                // data
                calendar = new TARGET();
                settlementDays = 2;
                today = new Date(9, Month.June, 2009);
                compounding = Compounding.Continuous;
                dayCount = new Actual360();
                settlementDate = calendar.advance(today, settlementDays, TimeUnit.Days);

                Settings.setEvaluationDate(today);

                var ts = new int[] { 13, 41, 75, 165, 256, 345, 524, 703 };
                var r = new double[] { 0.035, 0.033, 0.034, 0.034, 0.036, 0.037, 0.039, 0.040 };
                var rates = new List<double>() { 0.035 };
                var dates = new List<Date>() { settlementDate };
                for (var i = 0; i < 8; ++i)
                {
                    dates.Add(calendar.advance(today, ts[i], TimeUnit.Days));
                    rates.Add(r[i]);
                }
                termStructure = new InterpolatedZeroCurve<Linear>(dates, rates, dayCount);
            }
        }

        [Fact]
        public void testFlatInterpolationLeft()
        {
            // Testing flat interpolation before the first spreaded date...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

            var interpolationDate = vars.calendar.advance(vars.today, 6, TimeUnit.Months);

            ZeroYieldStructure spreadedTermStructure =
               new PiecewiseZeroSpreadedTermStructure(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() + spread1.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail("unable to reproduce interpolated rate\n"
                             + "    calculated: " + interpolatedZeroRate + "\n"
                             + "    expected: " + expectedRate);
        }

        [Fact]
        public void testFlatInterpolationRight()
        {
            // Testing flat interpolation after the last spreaded date...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

            var interpolationDate = vars.calendar.advance(vars.today, 20, TimeUnit.Months);

            ZeroYieldStructure spreadedTermStructure =
               new PiecewiseZeroSpreadedTermStructure(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);
            spreadedTermStructure.enableExtrapolation();

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() + spread2.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail("unable to reproduce interpolated rate\n"
                             + "    calculated: " + interpolatedZeroRate + "\n"
                             + "    expected: " + expectedRate);
        }

        [Fact]
        public void testLinearInterpolationMultipleSpreads()
        {
            // Testing linear interpolation with more than two spreaded dates...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.02);
            var spread3 = new SimpleQuote(0.035);
            var spread4 = new SimpleQuote(0.04);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));
            spreads.Add(new Handle<Quote>(spread3));
            spreads.Add(new Handle<Quote>(spread4));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 90, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 30, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 40, TimeUnit.Months));

            var interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new PiecewiseZeroSpreadedTermStructure(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"

                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }

        [Fact]
        public void testLinearInterpolation()
        {
            // Testing linear interpolation between two dates...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));

            var interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new InterpolatedPiecewiseZeroSpreadedTermStructure<Linear>(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var d0 = vars.calendar.advance(vars.today, 100, TimeUnit.Days);
            var d1 = vars.calendar.advance(vars.today, 150, TimeUnit.Days);
            var d2 = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

            var m = (0.03 - 0.02) / vars.dayCount.yearFraction(d0, d1);
            var expectedRate = m * vars.dayCount.yearFraction(d0, d2) + 0.054;

            var t = vars.dayCount.yearFraction(vars.settlementDate, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }

        [Fact]
        public void testForwardFlatInterpolation()
        {
            // Testing forward flat interpolation between two dates...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 75, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 260, TimeUnit.Days));

            var interpolationDate = vars.calendar.advance(vars.today, 100, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new InterpolatedPiecewiseZeroSpreadedTermStructure<ForwardFlat>(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }

        [Fact]
        public void testBackwardFlatInterpolation()
        {
            // Testing backward flat interpolation between two dates...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            var spread3 = new SimpleQuote(0.04);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));
            spreads.Add(new Handle<Quote>(spread3));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 200, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 300, TimeUnit.Days));

            var interpolationDate = vars.calendar.advance(vars.today, 110, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new InterpolatedPiecewiseZeroSpreadedTermStructure<BackwardFlat>(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread2.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);

        }

        [Fact]
        public void testDefaultInterpolation()
        {
            // Testing default interpolation between two dates...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.02);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 75, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 160, TimeUnit.Days));

            var interpolationDate = vars.calendar.advance(vars.today, 100, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new PiecewiseZeroSpreadedTermStructure(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }

        [Fact]
        public void testSetInterpolationFactory()
        {
            // Testing factory constructor with additional parameters...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            var spread3 = new SimpleQuote(0.01);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));
            spreads.Add(new Handle<Quote>(spread3));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 25, TimeUnit.Months));

            var interpolationDate = vars.calendar.advance(vars.today, 11, TimeUnit.Months);

            ZeroYieldStructure spreadedTermStructure;

            var freq = Frequency.NoFrequency;

            var factory = new Cubic(CubicInterpolation.DerivativeApprox.Spline,
                                      false,
                                      CubicInterpolation.BoundaryCondition.SecondDerivative, 0,
                                      CubicInterpolation.BoundaryCondition.SecondDerivative, 0);

            spreadedTermStructure =
               new InterpolatedPiecewiseZeroSpreadedTermStructure<Cubic>(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates, vars.compounding,
               freq, vars.dayCount, factory);

            var t = vars.dayCount.yearFraction(vars.today, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               0.026065770863;

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }

        [Fact]
        public void testMaxDate()
        {
            // Testing term structure max date...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
            spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

            ZeroYieldStructure spreadedTermStructure =
               new PiecewiseZeroSpreadedTermStructure(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var maxDate = spreadedTermStructure.maxDate();

            var expectedDate = vars.termStructure.maxDate() < spreadDates.Last() ? vars.termStructure.maxDate() : spreadDates.Last();

            if (maxDate != expectedDate)
                QAssert.Fail(
                   "unable to reproduce max date\n"
                   + "    calculated: " + maxDate + "\n"
                   + "    expected: " + expectedDate);
        }

        [Fact]
        public void testQuoteChanging()
        {
            // Testing quote update...

            var vars = new CommonVars();

            var spreads = new List<Handle<Quote>>();
            var spread1 = new SimpleQuote(0.02);
            var spread2 = new SimpleQuote(0.03);
            spreads.Add(new Handle<Quote>(spread1));
            spreads.Add(new Handle<Quote>(spread2));

            var spreadDates = new List<Date>();
            spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
            spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));

            var interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

            ZeroYieldStructure spreadedTermStructure =
               new InterpolatedPiecewiseZeroSpreadedTermStructure<BackwardFlat>(
               new Handle<YieldTermStructure>(vars.termStructure),
               spreads, spreadDates);

            var t = vars.dayCount.yearFraction(vars.settlementDate, interpolationDate);
            var interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();
            var tolerance = 1e-9;
            var expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               0.03;

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);

            spread2.setValue(0.025);

            interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();
            expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                           0.025;

            if (System.Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
                QAssert.Fail(
                   "unable to reproduce interpolated rate\n"
                   + "    calculated: " + interpolatedZeroRate + "\n"
                   + "    expected: " + expectedRate);
        }
    }
}
