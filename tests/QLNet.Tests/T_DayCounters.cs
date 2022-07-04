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
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Calendar = QLNet.Time.Calendar;
using QLNet.Time;
using QLNet.Time.DayCounters;
using QLNet.Extensions;
using QLNet.Time.Calendars;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_DayCounters
    {
        public struct SingleCase
        {
            public SingleCase(ActualActual.Convention convention, Date start, Date end, Date refStart, Date refEnd, double result)
            {
                _convention = convention;
                _start = start;
                _end = end;
                _refStart = refStart;
                _refEnd = refEnd;
                _result = result;
            }
            public SingleCase(ActualActual.Convention convention, Date start, Date end, double result)
            {
                _convention = convention;
                _start = start;
                _end = end;
                _refStart = new Date();
                _refEnd = new Date();
                _result = result;
            }
            public ActualActual.Convention _convention;
            public Date _start;
            public Date _end;
            public Date _refStart;
            public Date _refEnd;
            public double _result;
        }

        private double actualActualDaycountComputation(Schedule schedule,
                                                       Date start, Date end)
        {

            DayCounter daycounter = new ActualActual(ActualActual.Convention.ISMA, schedule);
            var yearFraction = 0.0;

            for (var i = 1; i < schedule.size() - 1; i++)
            {
                var referenceStart = schedule.date(i);
                var referenceEnd = schedule.date(i + 1);
                if (start < referenceEnd && end > referenceStart)
                {
                    yearFraction += ISMAYearFractionWithReferenceDates(
                                       daycounter,
                                       start > referenceStart ? start : referenceStart,
                                       end < referenceEnd ? end : referenceEnd,
                                       referenceStart,
                                       referenceEnd
                                    );
                };
            }
            return yearFraction;
        }

        private double ISMAYearFractionWithReferenceDates(DayCounter dayCounter, Date start, Date end,
                                                          Date refStart, Date refEnd)
        {
            double referenceDayCount = dayCounter.dayCount(refStart, refEnd);
            // guess how many coupon periods per year:
            var couponsPerYear = (int)(0.5 + 365.0 / referenceDayCount);
            // the above is good enough for annual or semi annual payments.
            return dayCounter.dayCount(start, end)
                   / (referenceDayCount * couponsPerYear);
        }

        [Fact]
        public void testActualActual()
        {
            SingleCase[] testCases =
            {
            // first example
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.497724380567),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.497267759563),
            // short first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           0.410958904110),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           new Date(1, Month.July, 1998), new Date(1, Month.July, 1999),
                           0.410958904110),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           0.410958904110),
            // short first calculation period (second period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.001377348600),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.000000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.000000000000),
            // long first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           0.915068493151),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           new Date(15, Month.January, 2003), new Date(15, Month.July, 2003),
                           0.915760869565),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           0.915068493151),
            // long first calculation period (second period)
            /* Warning: the ISDA case is in disagreement with mktc1198.pdf */
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.504004790778),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.504109589041),
            // short final calculation period (penultimate period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.503892506924),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.504109589041),
            // short final calculation period (final period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
                           0.415300546448),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
                           new Date(30, Month.January, 2000), new Date(30, Month.July, 2000),
                           0.417582417582),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
                           0.41530054644)
         };

            var n = testCases.Length; /// sizeof(SingleCase);
            for (var i = 0; i < n; i++)
            {
                var dayCounter = new ActualActual(testCases[i]._convention);
                var d1 = testCases[i]._start;
                var d2 = testCases[i]._end;
                var rd1 = testCases[i]._refStart;
                var rd2 = testCases[i]._refEnd;
                var calculated = dayCounter.yearFraction(d1, d2, rd1, rd2);

                if (System.Math.Abs(calculated - testCases[i]._result) > 1.0e-10)
                {
                    QAssert.Fail(dayCounter.name() + "period: " + d1 + " to " + d2 +
                                 "    calculated: " + calculated + "    expected:   " + testCases[i]._result);
                }
            }
        }

        [Fact]
        public void testActualActualWithSemiannualSchedule()
        {

            // Testing actual/actual with schedule for undefined semiannual reference periods

            Calendar calendar = new UnitedStates();
            var fromDate = new Date(10, Month.January, 2017);
            var firstCoupon = new Date(31, Month.August, 2017);
            var quasiCoupon = new Date(28, Month.February, 2017);
            var quasiCoupon2 = new Date(31, Month.August, 2016);

            var schedule = new MakeSchedule()
            .from(fromDate)
            .withFirstDate(firstCoupon)
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(true).value();

            var testDate = schedule.date(1);
            DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);
            DayCounter dayCounterNoSchedule = new ActualActual(ActualActual.Convention.ISMA);

            var referencePeriodStart = schedule.date(1);
            var referencePeriodEnd = schedule.date(2);

            // Test
            QAssert.IsTrue(dayCounter.yearFraction(referencePeriodStart,
                                                   referencePeriodStart).IsEqual(0.0), "This should be zero.");

            QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                             referencePeriodStart).IsEqual(0.0), "This should be zero");

            QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                             referencePeriodStart, referencePeriodStart, referencePeriodStart).IsEqual(0.0),
                           "This should be zero");

            QAssert.IsTrue(dayCounter.yearFraction(referencePeriodStart,
                                                   referencePeriodEnd).IsEqual(0.5),
                           "This should be exact using schedule; "
                           + referencePeriodStart + " to " + referencePeriodEnd
                           + "Should be 0.5");

            QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                             referencePeriodEnd, referencePeriodStart, referencePeriodEnd).IsEqual(0.5),
                           "This should be exact for explicit reference periods with no schedule");

            while (testDate < referencePeriodEnd)
            {
                var difference =
                   dayCounter.yearFraction(testDate, referencePeriodEnd,
                                           referencePeriodStart, referencePeriodEnd) -
                   dayCounter.yearFraction(testDate, referencePeriodEnd);
                if (System.Math.Abs(difference) > 1.0e-10)
                {
                    QAssert.Fail("Failed to correctly use the schedule to find the reference period for Act/Act");
                }
                testDate = calendar.advance(testDate, 1, TimeUnit.Days);
            }

            //Test long first coupon
            var calculatedYearFraction =
               dayCounter.yearFraction(fromDate, firstCoupon);
            var expectedYearFraction =
               0.5 + (double)dayCounter.dayCount(fromDate, quasiCoupon)
               / (2 * dayCounter.dayCount(quasiCoupon2, quasiCoupon));

            QAssert.IsTrue(System.Math.Abs(calculatedYearFraction - expectedYearFraction) < 1.0e-10,
                           "Failed to compute the expected year fraction " +
                           "\n expected:   " + expectedYearFraction +
                           "\n calculated: " + calculatedYearFraction);

            // test multiple periods

            schedule = new MakeSchedule()
            .from(new Date(10, Month.January, 2017))
            .withFirstDate(new Date(31, Month.August, 2017))
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(false).value();

            var periodStartDate = schedule.date(1);
            var periodEndDate = schedule.date(2);

            dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

            while (periodEndDate < schedule.date(schedule.size() - 2))
            {
                var expected =
                   actualActualDaycountComputation(schedule,
                                                   periodStartDate,
                                                   periodEndDate);
                var calculated = dayCounter.yearFraction(periodStartDate,
                                                            periodEndDate);

                if (System.Math.Abs(expected - calculated) > 1e-8)
                {
                    QAssert.Fail("Failed to compute the correct year fraction " +
                                 "given a schedule: " + periodStartDate +
                                 " to " + periodEndDate +
                                 "\n expected: " + expected +
                                 " calculated: " + calculated);
                }
                periodEndDate = calendar.advance(periodEndDate, 1, TimeUnit.Days);
            }
        }

        [Fact]
        public void testActualActualWithAnnualSchedule()
        {
            // Testing actual/actual with schedule "for undefined annual reference periods

            // Now do an annual schedule
            Calendar calendar = new UnitedStates();
            var schedule = new MakeSchedule()
            .from(new Date(10, Month.January, 2017))
            .withFirstDate(new Date(31, Month.August, 2017))
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Annual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(false).value();

            var referencePeriodStart = schedule.date(1);
            var referencePeriodEnd = schedule.date(2);

            var testDate = schedule.date(1);
            DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

            while (testDate < referencePeriodEnd)
            {
                var difference =
                   ISMAYearFractionWithReferenceDates(dayCounter,
                                                      testDate, referencePeriodEnd,
                                                      referencePeriodStart, referencePeriodEnd) -
                   dayCounter.yearFraction(testDate, referencePeriodEnd);
                if (System.Math.Abs(difference) > 1.0e-10)
                {
                    QAssert.Fail("Failed to correctly use the schedule " +
                                 "to find the reference period for Act/Act:\n"
                                 + testDate + " to " + referencePeriodEnd
                                 + "\n Ref: " + referencePeriodStart
                                 + " to " + referencePeriodEnd);
                }
                testDate = calendar.advance(testDate, 1, TimeUnit.Days);
            }
        }

        [Fact]
        public void testActualActualWithSchedule()
        {

            // Testing actual/actual day counter with schedule

            // long first coupon
            var issueDateExpected = new Date(17, Month.January, 2017);
            var firstCouponDateExpected = new Date(31, Month.August, 2017);

            var schedule =
               new MakeSchedule()
            .from(issueDateExpected)
            .withFirstDate(firstCouponDateExpected)
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(new Canada())
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards()
            .endOfMonth().value();

            var issueDate = schedule.date(0);
            Utils.QL_REQUIRE(issueDate == issueDateExpected, () =>
                             "This is not the expected issue date " + issueDate
                             + " expected " + issueDateExpected);
            var firstCouponDate = schedule.date(1);
            Utils.QL_REQUIRE(firstCouponDate == firstCouponDateExpected, () =>
                             "This is not the expected first coupon date " + firstCouponDate
                             + " expected: " + firstCouponDateExpected);

            //Make thw quasi coupon dates:
            var quasiCouponDate2 = schedule.calendar().advance(firstCouponDate,
                                                                -schedule.tenor(),
                                                                schedule.businessDayConvention(),
                                                                schedule.endOfMonth());
            var quasiCouponDate1 = schedule.calendar().advance(quasiCouponDate2,
                                                                -schedule.tenor(),
                                                                schedule.businessDayConvention(),
                                                                schedule.endOfMonth());

            var quasiCouponDate1Expected = new Date(31, Month.August, 2016);
            var quasiCouponDate2Expected = new Date(28, Month.February, 2017);

            Utils.QL_REQUIRE(quasiCouponDate2 == quasiCouponDate2Expected, () =>
                             "Expected " + quasiCouponDate2Expected
                             + " as the later quasi coupon date but received "
                             + quasiCouponDate2);
            Utils.QL_REQUIRE(quasiCouponDate1 == quasiCouponDate1Expected, () =>
                             "Expected " + quasiCouponDate1Expected
                             + " as the earlier quasi coupon date but received "
                             + quasiCouponDate1);

            DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

            // full coupon
            var t_with_reference = dayCounter.yearFraction(
                                         issueDate, firstCouponDate,
                                         quasiCouponDate2, firstCouponDate
                                      );
            var t_no_reference = dayCounter.yearFraction(
                                       issueDate,
                                       firstCouponDate
                                    );
            var t_total =
               ISMAYearFractionWithReferenceDates(dayCounter,
                                                  issueDate, quasiCouponDate2,
                                                  quasiCouponDate1, quasiCouponDate2)
               + 0.5;
            var expected = 0.6160220994;


            if (System.Math.Abs(t_total - expected) > 1.0e-10)
            {
                QAssert.Fail("Failed to reproduce expected time:\n"
                             + "    calculated: " + t_total + "\n"
                             + "    expected:   " + expected);
            }
            if (System.Math.Abs(t_with_reference - expected) > 1.0e-10)
            {
                QAssert.Fail("Failed to reproduce expected time:\n"
                             + "    calculated: " + t_with_reference + "\n"
                             + "    expected:   " + expected);
            }
            if (System.Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
            {
                QAssert.Fail("Should produce the same time whether or not references are present");
            }

            // settlement date in the first quasi-period
            var settlementDate = new Date(29, Month.January, 2017);

            t_with_reference = ISMAYearFractionWithReferenceDates(
                                  dayCounter,
                                  issueDate, settlementDate,
                                  quasiCouponDate1, quasiCouponDate2
                               );
            t_no_reference = dayCounter.yearFraction(issueDate, settlementDate);
            var t_expected_first_qp = 0.03314917127071823; //12.0/362
            if (System.Math.Abs(t_with_reference - t_expected_first_qp) > 1.0e-10)
            {
                QAssert.Fail("Failed to reproduce expected time:\n"
                             + "    calculated: " + t_no_reference + "\n"
                             + "    expected:   " + t_expected_first_qp);
            }
            if (System.Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
            {
                QAssert.Fail("Should produce the same time whether or not references are present");
            }
            var t2 = dayCounter.yearFraction(settlementDate, firstCouponDate);
            if (System.Math.Abs(t_expected_first_qp + t2 - expected) > 1.0e-10)
            {
                QAssert.Fail("Sum of quasiperiod2 split is not consistent");
            }

            // settlement date in the second quasi-period
            settlementDate = new Date(29, Month.July, 2017);
            t_no_reference = dayCounter.yearFraction(issueDate, settlementDate);
            t_with_reference = ISMAYearFractionWithReferenceDates(
                                  dayCounter,
                                  issueDate, quasiCouponDate2,
                                  quasiCouponDate1, quasiCouponDate2
                               ) + ISMAYearFractionWithReferenceDates(
                                  dayCounter,
                                  quasiCouponDate2, settlementDate,
                                  quasiCouponDate2, firstCouponDate
                               );
            if (System.Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
            {
                QAssert.Fail("These two cases should be identical");
            }
            t2 = dayCounter.yearFraction(settlementDate, firstCouponDate);
            if (System.Math.Abs(t_total - (t_no_reference + t2)) > 1.0e-10)
            {
                QAssert.Fail("Failed to reproduce expected time:\n"
                             + "    calculated: " + t_total + "\n"
                             + "    expected:   " + t_no_reference + t2);
            }
        }

        [Fact]
        public void testSimple()
        {
            Period[] p = { new Period(3, TimeUnit.Months), new Period(6, TimeUnit.Months), new Period(1, TimeUnit.Years) };
            double[] expected = { 0.25, 0.5, 1.0 };
            var n = p.Length;

            // 4 years should be enough
            Date first = new Date(1, Month.January, 2002), last = new Date(31, Month.December, 2005);
            DayCounter dayCounter = new SimpleDayCounter();

            for (var start = first; start <= last; start++)
            {
                for (var i = 0; i < n; i++)
                {
                    var end = start + p[i];
                    var calculated = dayCounter.yearFraction(start, end, null, null);
                    if (System.Math.Abs(calculated - expected[i]) > 1.0e-12)
                    {
                        QAssert.Fail("from " + start + " to " + end +
                                     "Calculated: " + calculated +
                                     "Expected:   " + expected[i]);
                    }
                }
            }

        }

        [Fact]
        public void testOne()
        {
            Period[] p = { new Period(3, TimeUnit.Months), new Period(6, TimeUnit.Months), new Period(1, TimeUnit.Years) };
            double[] expected = { 1.0, 1.0, 1.0 };
            var n = p.Length;

            // 1 years should be enough
            Date first = new Date(1, Month.January, 2004), last = new Date(31, Month.December, 2004);
            DayCounter dayCounter = new OneDayCounter();

            for (var start = first; start <= last; start++)
            {
                for (var i = 0; i < n; i++)
                {
                    var end = start + p[i];
                    var calculated = dayCounter.yearFraction(start, end, null, null);
                    if (System.Math.Abs(calculated - expected[i]) > 1.0e-12)
                    {
                        QAssert.Fail("from " + start + " to " + end +
                                     "Calculated: " + calculated +
                                     "Expected:   " + expected[i]);
                    }
                }
            }

        }

        [Fact]
        public void testBusiness252()
        {
            // Testing business/252 day counter

            var testDates = new List<Date>();
            testDates.Add(new Date(1, Month.February, 2002));
            testDates.Add(new Date(4, Month.February, 2002));
            testDates.Add(new Date(16, Month.May, 2003));
            testDates.Add(new Date(17, Month.December, 2003));
            testDates.Add(new Date(17, Month.December, 2004));
            testDates.Add(new Date(19, Month.December, 2005));
            testDates.Add(new Date(2, Month.January, 2006));
            testDates.Add(new Date(13, Month.March, 2006));
            testDates.Add(new Date(15, Month.May, 2006));
            testDates.Add(new Date(17, Month.March, 2006));
            testDates.Add(new Date(15, Month.May, 2006));
            testDates.Add(new Date(26, Month.July, 2006));
            testDates.Add(new Date(28, Month.June, 2007));
            testDates.Add(new Date(16, Month.September, 2009));
            testDates.Add(new Date(26, Month.July, 2016));

            double[] expected =
            {
            0.0039682539683,
            1.2738095238095,
            0.6031746031746,
            0.9960317460317,
            1.0000000000000,
            0.0396825396825,
            0.1904761904762,
            0.1666666666667,
            -0.1507936507937,
            0.1507936507937,
            0.2023809523810,
            0.912698412698,
            2.214285714286,
            6.84126984127
         };

            DayCounter dayCounter1 = new Business252(new Brazil());

            double calculated;

            for (var i = 1; i < testDates.Count; i++)
            {
                calculated = dayCounter1.yearFraction(testDates[i - 1], testDates[i]);
                if (System.Math.Abs(calculated - expected[i - 1]) > 1.0e-12)
                {
                    QAssert.Fail("from " + testDates[i - 1]
                                 + " to " + testDates[i] + ":\n"
                                 + "    calculated: " + calculated + "\n"
                                 + "    expected:   " + expected[i - 1]);
                }
            }

            DayCounter dayCounter2 = new Business252();

            for (var i = 1; i < testDates.Count; i++)
            {
                calculated = dayCounter2.yearFraction(testDates[i - 1], testDates[i]);
                if (System.Math.Abs(calculated - expected[i - 1]) > 1.0e-12)
                {
                    QAssert.Fail("from " + testDates[i - 1]
                                 + " to " + testDates[i] + ":\n"
                                 + "    calculated: " + calculated + "\n"
                                 + "    expected:   " + expected[i - 1]);

                }
            }
        }

        [Fact]
        public void testThirty360_BondBasis()
        {
            // Testing thirty/360 day counter (Bond Basis)
            // http://www.isda.org/c_and_a/docs/30-360-2006ISDADefs.xls
            // Source: 2006 ISDA Definitions, Sec. 4.16 (f)
            // 30/360 (or Bond Basis)

            DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
            var testStartDates = new List<Date>();
            var testEndDates = new List<Date>();
            int calculated;

            // ISDA - Example 1: End dates do not involve the last day of February
            testStartDates.Add(new Date(20, Month.August, 2006)); testEndDates.Add(new Date(20, Month.February, 2007));
            testStartDates.Add(new Date(20, Month.February, 2007)); testEndDates.Add(new Date(20, Month.August, 2007));
            testStartDates.Add(new Date(20, Month.August, 2007)); testEndDates.Add(new Date(20, Month.February, 2008));
            testStartDates.Add(new Date(20, Month.February, 2008)); testEndDates.Add(new Date(20, Month.August, 2008));
            testStartDates.Add(new Date(20, Month.August, 2008)); testEndDates.Add(new Date(20, Month.February, 2009));
            testStartDates.Add(new Date(20, Month.February, 2009)); testEndDates.Add(new Date(20, Month.August, 2009));

            // ISDA - Example 2: End dates include some end-February dates
            testStartDates.Add(new Date(31, Month.August, 2006)); testEndDates.Add(new Date(28, Month.February, 2007));
            testStartDates.Add(new Date(28, Month.February, 2007)); testEndDates.Add(new Date(31, Month.August, 2007));
            testStartDates.Add(new Date(31, Month.August, 2007)); testEndDates.Add(new Date(29, Month.February, 2008));
            testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
            testStartDates.Add(new Date(31, Month.August, 2008)); testEndDates.Add(new Date(28, Month.February, 2009));
            testStartDates.Add(new Date(28, Month.February, 2009)); testEndDates.Add(new Date(31, Month.August, 2009));

            //// ISDA - Example 3: Miscellaneous calculations
            testStartDates.Add(new Date(31, Month.January, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(30, Month.January, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(28, Month.February, 2006)); testEndDates.Add(new Date(3, Month.March, 2006));
            testStartDates.Add(new Date(14, Month.February, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(30, Month.September, 2006)); testEndDates.Add(new Date(31, Month.October, 2006));
            testStartDates.Add(new Date(31, Month.October, 2006)); testEndDates.Add(new Date(28, Month.November, 2006));
            testStartDates.Add(new Date(31, Month.August, 2007)); testEndDates.Add(new Date(28, Month.February, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(28, Month.August, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(30, Month.August, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
            testStartDates.Add(new Date(26, Month.February, 2007)); testEndDates.Add(new Date(28, Month.February, 2008));
            testStartDates.Add(new Date(26, Month.February, 2007)); testEndDates.Add(new Date(29, Month.February, 2008));
            testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(28, Month.February, 2009));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(30, Month.March, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(31, Month.March, 2008));

            int[] expected = { 180, 180, 180, 180, 180, 180,
                            178, 183, 179, 182, 178, 183,
                            28,  28,   5,  14,  30,  28,
                            178, 180, 182, 183, 362, 363,
                            359,  32,  33
                          };

            for (var i = 0; i < testStartDates.Count; i++)
            {
                calculated = dayCounter.dayCount(testStartDates[i], testEndDates[i]);
                if (calculated != expected[i])
                {
                    QAssert.Fail("from " + testStartDates[i]
                                 + " to " + testEndDates[i] + ":\n"
                                 + "    calculated: " + calculated + "\n"
                                 + "    expected:   " + expected[i]);
                }
            }
        }

        [Fact]
        public void testThirty360_EurobondBasis()
        {
            // Testing thirty/360 day counter (Eurobond Basis)
            // Source: ISDA 2006 Definitions 4.16 (g)
            // 30E/360 (or Eurobond Basis)
            // Based on ICMA (Rule 251) and FBF; this is the version of 30E/360 used by Excel

            DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.EurobondBasis);
            var testStartDates = new List<Date>();
            var testEndDates = new List<Date>();
            int calculated;

            // ISDA - Example 1: End dates do not involve the last day of February
            testStartDates.Add(new Date(20, Month.August, 2006)); testEndDates.Add(new Date(20, Month.February, 2007));
            testStartDates.Add(new Date(20, Month.February, 2007)); testEndDates.Add(new Date(20, Month.August, 2007));
            testStartDates.Add(new Date(20, Month.August, 2007)); testEndDates.Add(new Date(20, Month.February, 2008));
            testStartDates.Add(new Date(20, Month.February, 2008)); testEndDates.Add(new Date(20, Month.August, 2008));
            testStartDates.Add(new Date(20, Month.August, 2008)); testEndDates.Add(new Date(20, Month.February, 2009));
            testStartDates.Add(new Date(20, Month.February, 2009)); testEndDates.Add(new Date(20, Month.August, 2009));

            //// ISDA - Example 2: End dates include some end-February dates
            testStartDates.Add(new Date(28, Month.February, 2006)); testEndDates.Add(new Date(31, Month.August, 2006));
            testStartDates.Add(new Date(31, Month.August, 2006)); testEndDates.Add(new Date(28, Month.February, 2007));
            testStartDates.Add(new Date(28, Month.February, 2007)); testEndDates.Add(new Date(31, Month.August, 2007));
            testStartDates.Add(new Date(31, Month.August, 2007)); testEndDates.Add(new Date(29, Month.February, 2008));
            testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
            testStartDates.Add(new Date(31, Month.August, 2008)); testEndDates.Add(new Date(28, Month.Feb, 2009));
            testStartDates.Add(new Date(28, Month.February, 2009)); testEndDates.Add(new Date(31, Month.August, 2009));
            testStartDates.Add(new Date(31, Month.August, 2009)); testEndDates.Add(new Date(28, Month.Feb, 2010));
            testStartDates.Add(new Date(28, Month.February, 2010)); testEndDates.Add(new Date(31, Month.August, 2010));
            testStartDates.Add(new Date(31, Month.August, 2010)); testEndDates.Add(new Date(28, Month.Feb, 2011));
            testStartDates.Add(new Date(28, Month.February, 2011)); testEndDates.Add(new Date(31, Month.August, 2011));
            testStartDates.Add(new Date(31, Month.August, 2011)); testEndDates.Add(new Date(29, Month.Feb, 2012));

            //// ISDA - Example 3: Miscellaneous calculations
            testStartDates.Add(new Date(31, Month.January, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(30, Month.January, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(28, Month.February, 2006)); testEndDates.Add(new Date(3, Month.March, 2006));
            testStartDates.Add(new Date(14, Month.February, 2006)); testEndDates.Add(new Date(28, Month.February, 2006));
            testStartDates.Add(new Date(30, Month.September, 2006)); testEndDates.Add(new Date(31, Month.October, 2006));
            testStartDates.Add(new Date(31, Month.October, 2006)); testEndDates.Add(new Date(28, Month.November, 2006));
            testStartDates.Add(new Date(31, Month.August, 2007)); testEndDates.Add(new Date(28, Month.February, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(28, Month.August, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(30, Month.August, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
            testStartDates.Add(new Date(26, Month.February, 2007)); testEndDates.Add(new Date(28, Month.February, 2008));
            testStartDates.Add(new Date(26, Month.February, 2007)); testEndDates.Add(new Date(29, Month.February, 2008));
            testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(28, Month.February, 2009));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(30, Month.March, 2008));
            testStartDates.Add(new Date(28, Month.February, 2008)); testEndDates.Add(new Date(31, Month.March, 2008));

            int[] expected = { 180, 180, 180, 180, 180, 180,
                            182, 178, 182, 179, 181, 178,
                            182, 178, 182, 178, 182, 179,
                            28,  28,   5,  14,  30,  28,
                            178, 180, 182, 182, 362, 363,
                            359,  32,  32
                          };

            for (var i = 0; i < testStartDates.Count; i++)
            {
                calculated = dayCounter.dayCount(testStartDates[i], testEndDates[i]);
                if (calculated != expected[i])
                {
                    QAssert.Fail("from " + testStartDates[i]
                                 + " to " + testEndDates[i] + ":\n"
                                 + "    calculated: " + calculated + "\n"
                                 + "    expected:   " + expected[i]);
                }
            }
        }

        [Fact]
        public void testIntraday()
        {
            // Testing intraday behavior of day counter

            var d1 = new Date(12, Month.February, 2015);
            var d2 = new Date(14, Month.February, 2015, 12, 34, 17, 1);

            var tol = 100 * Const.QL_EPSILON;

            DayCounter[] dayCounters = { new ActualActual(), new Actual365Fixed(), new Actual360() };

            for (var i = 0; i < dayCounters.Length; ++i)
            {
                var dc = dayCounters[i];

                var expected = ((12 * 60 + 34) * 60 + 17 + 0.001)
                               * dc.yearFraction(d1, d1 + 1) / 86400
                               + dc.yearFraction(d1, d1 + 2);

                QAssert.IsTrue(System.Math.Abs(dc.yearFraction(d1, d2) - expected) < tol,
                               "can not reproduce result for day counter " + dc.name());

                QAssert.IsTrue(System.Math.Abs(dc.yearFraction(d2, d1) + expected) < tol,
                               "can not reproduce result for day counter " + dc.name());
            }
        }

        /// <summary>
        /// https://www.isda.org/book/actualactual-day-count-fraction/
        /// </summary>
        /// <param name="isEndOfMonth"></param>
        /// <param name="frequency"></param>
        /// <param name="interestAccrualDateAsString"></param>
        /// <param name="maturityDateAsString"></param>
        /// <param name="firstCouponDateAsString"></param>
        /// <param name="penultimateCouponDateAsString"></param>
        /// <param name="d1AsString"></param>
        /// <param name="d2AsString"></param>
        /// <param name="expectedYearFraction"></param>
        [Theory]
        [InlineData(false, Frequency.Semiannual, "2003-05-01", "2005-05-01", "2003-11-01", "2004-11-01", "2003-11-01", "2004-05-01", 182.0 / (182.0 * 2))] // example a: regular calculation period
        [InlineData(false, Frequency.Annual, "1999-02-01", "2002-07-01", "1999-07-01", "2001-07-01", "1999-02-01", "1999-07-01", 150.0 / (365.0 * 1))] // example b: short first calculation period - first period
        [InlineData(false, Frequency.Annual, "1999-02-01", "2002-07-01", "1999-07-01", "2001-07-01", "1999-07-01", "2000-07-01", 366.0 / (366.0 * 1))] // example b: short first calculation period - second period
        [InlineData(false, Frequency.Semiannual, "2002-08-15", "2005-07-15", "2003-07-15", "2004-07-15", "2002-08-15", "2003-07-15", 181.0 / (181.0 * 2) + 153.0 / (184.0 * 2))] // example c: long first calculation period - first period
        [InlineData(false, Frequency.Semiannual, "2002-08-15", "2005-07-15", "2003-07-15", "2004-07-15", "2003-07-15", "2004-01-15", 184.0 / (184.0 * 2))] // example c: long first calculation period - second period
        [InlineData(false, Frequency.Semiannual, "1999-01-30", "2000-06-30", "1999-07-30", "2000-01-30", "1999-07-30", "2000-01-30", 184.0 / (184.0 * 2))] // example d: short final calculation period - penultimate period
        [InlineData(false, Frequency.Semiannual, "1999-01-30", "2000-06-30", "1999-07-30", "2000-01-30", "2000-01-30", "2000-06-30", 152.0 / (182.0 * 2))] // example d: short final calculation period - final period
        [InlineData(true, Frequency.Quarterly, "1999-05-31", "2000-04-30", "1999-08-31", "1999-11-30", "1999-11-30", "2000-04-30", 91.0 / (91.0 * 4) + 61.0 / (92.0 * 4))] // example e: long final calculation period
        [InlineData(false, Frequency.Quarterly, "1999-05-31", "2000-04-30", "1999-08-31", "1999-11-30", "1999-11-30", "2000-04-30", 91.0 / (91.0 * 4) + 61.0 / (90.0 * 4))] // example e: long final calculation period - not end of month
        public void testActualActualIsma(bool isEndOfMonth, Frequency frequency, string interestAccrualDateAsString, string maturityDateAsString, string firstCouponDateAsString, string penultimateCouponDateAsString, string d1AsString, string d2AsString, double expectedYearFraction)
        {
            // Example from ISDA Paper: The actual/actual day count fraction, paper for use with the ISDA Market Conventions Survey, 3rd June, 1999
            var interestAccrualDate = new Date(DateTime.ParseExact(interestAccrualDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            var maturityDate = new Date(DateTime.ParseExact(maturityDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            var firstCouponDate = new Date(DateTime.ParseExact(firstCouponDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            var penultimateCouponDate = new Date(DateTime.ParseExact(penultimateCouponDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));

            var d1 = new Date(DateTime.ParseExact(d1AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            var d2 = new Date(DateTime.ParseExact(d2AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));

            var schedule = new MakeSchedule()
                .from(interestAccrualDate)
                .to(maturityDate)
                .withFrequency(frequency)
                .withFirstDate(firstCouponDate)
                .withNextToLastDate(penultimateCouponDate)
                .endOfMonth(isEndOfMonth)
                .value();

            var dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

            var t = dayCounter.yearFraction(d1, d2);

            Assert.Equal(expectedYearFraction, t);
        }
    }
}
