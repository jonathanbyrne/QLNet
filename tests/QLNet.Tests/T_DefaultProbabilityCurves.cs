/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using Xunit;
using QLNet.Time;
using QLNet.Quotes;
using QLNet.Termstructures.Credit;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_DefaultProbabilityCurves
    {
        [Fact]
        public void testDefaultProbability()
        {
            // Testing default-probability structure...

            var hazardRate = 0.0100;
            var hazardRateQuote = new Handle<Quote>(new SimpleQuote(hazardRate));
            DayCounter dayCounter = new Actual360();
            Calendar calendar = new TARGET();
            var n = 20;

            var tolerance = 1.0e-10;
            var today = Settings.evaluationDate();
            var startDate = today;
            var endDate = startDate;

            var flatHazardRate = new FlatHazardRate(startDate, hazardRateQuote, dayCounter);

            for (var i = 0; i < n; i++)
            {
                startDate = endDate;
                endDate = calendar.advance(endDate, 1, TimeUnit.Years);

                var pStart = flatHazardRate.defaultProbability(startDate);
                var pEnd = flatHazardRate.defaultProbability(endDate);

                var pBetweenComputed =
                   flatHazardRate.defaultProbability(startDate, endDate);

                var pBetween = pEnd - pStart;

                if (System.Math.Abs(pBetween - pBetweenComputed) > tolerance)
                {
                    QAssert.Fail("Failed to reproduce probability(d1, d2) "
                                 + "for default probability structure\n"
                                 + "    calculated probability: " + pBetweenComputed + "\n"
                                 + "    expected probability:   " + pBetween);
                }

                var t2 = dayCounter.yearFraction(today, endDate);
                var timeProbability = flatHazardRate.defaultProbability(t2);
                var dateProbability =
                   flatHazardRate.defaultProbability(endDate);

                if (System.Math.Abs(timeProbability - dateProbability) > tolerance)
                {
                    QAssert.Fail("single-time probability and single-date probability do not match\n"
                                 + "    time probability: " + timeProbability + "\n"
                                 + "    date probability: " + dateProbability);
                }

                var t1 = dayCounter.yearFraction(today, startDate);
                timeProbability = flatHazardRate.defaultProbability(t1, t2);
                dateProbability = flatHazardRate.defaultProbability(startDate, endDate);

                if (System.Math.Abs(timeProbability - dateProbability) > tolerance)
                {
                    QAssert.Fail("double-time probability and double-date probability do not match\n"
                                 + "    time probability: " + timeProbability + "\n"
                                 + "    date probability: " + dateProbability);
                }
            }
        }

        [Fact]
        public void testFlatHazardRate()
        {

            // Testing flat hazard rate...

            var hazardRate = 0.0100;
            var hazardRateQuote = new Handle<Quote>(new SimpleQuote(hazardRate));
            DayCounter dayCounter = new Actual360();
            Calendar calendar = new TARGET();
            var n = 20;

            var tolerance = 1.0e-10;
            var today = Settings.evaluationDate();
            var startDate = today;
            var endDate = startDate;

            var flatHazardRate = new FlatHazardRate(today, hazardRateQuote, dayCounter);

            for (var i = 0; i < n; i++)
            {
                endDate = calendar.advance(endDate, 1, TimeUnit.Years);
                var t = dayCounter.yearFraction(startDate, endDate);
                var probability = 1.0 - System.Math.Exp(-hazardRate * t);
                var computedProbability = flatHazardRate.defaultProbability(t);

                if (System.Math.Abs(probability - computedProbability) > tolerance)
                {
                    QAssert.Fail("Failed to reproduce probability for flat hazard rate\n"
                                 + "    calculated probability: " + computedProbability + "\n"
                                 + "    expected probability:   " + probability);
                }
            }
        }

        [Fact]
        public void testFlatHazardConsistency()
        {
            // Testing piecewise-flat hazard-rate consistency...
            //testBootstrapFromSpread<HazardRate,BackwardFlat>();
            //testBootstrapFromUpfront<HazardRate,BackwardFlat>();
        }
    }
}
