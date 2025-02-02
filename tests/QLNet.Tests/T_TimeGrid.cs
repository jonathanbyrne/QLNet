﻿//  Copyright (C) 2008-2019 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System;
using System.Collections.Generic;
using Xunit;
using System.Diagnostics;
using QLNet.Extensions;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_TimeGrid
    {
        [Fact]
        public void testConstructorAdditionalSteps()
        {
            // Testing TimeGrid construction with additional steps
            var test_times = new List<double> { 1.0, 2.0, 4.0 };
            var tg = new TimeGrid(test_times, 8);

            // Expect 8 evenly sized steps over the interval [0, 4].
            var expected_times = new List<double>
         {
            0.0,
            0.5,
            1.0,
            1.5,
            2.0,
            2.5,
            3.0,
            3.5,
            4.0
         };

            QAssert.CollectionAreEqual(tg.Times(), expected_times);
        }

        [Fact]
        public void testConstructorMandatorySteps()
        {
            // Testing TimeGrid construction with only mandatory points
            var test_times = new List<double> { 0.0, 1.0, 2.0, 4.0 };
            var tg = new TimeGrid(test_times);

            // Time grid must include all times from passed iterator.
            // Further no additional times can be added.
            QAssert.CollectionAreEqual(tg.Times(), test_times);
        }

        [Fact]
        public void testConstructorEvenSteps()
        {
            // Testing TimeGrid construction with n evenly spaced points

            double end_time = 10;
            var steps = 5;
            var tg = new TimeGrid(end_time, steps);
            var expected_times = new List<double>
         {
            0.0,
            2.0,
            4.0,
            6.0,
            8.0,
            10.0
         };

            QAssert.CollectionAreEqual(tg.Times(), expected_times);
        }

        [Fact]
        public void testConstructorEmptyIterator()
        {
            // Testing that the TimeGrid constructor raises an error for empty iterators

            var times = new List<double>();

            QAssert.ThrowsException<ArgumentException>(() => new TimeGrid(times));
        }

        [Fact]
        public void testConstructorNegativeValuesInIterator()
        {
            // Testing that the TimeGrid constructor raises an error for negative time values
            var times = new List<double> { -3.0, 1.0, 4.0, 5.0 };
            QAssert.ThrowsException<ArgumentException>(() => new TimeGrid(times));
        }

        [Fact]
        public void testClosestIndex()
        {
            // Testing that the returned index is closest to the requested time
            var test_times = new List<double> { 1.0, 2.0, 5.0 };
            var tg = new TimeGrid(test_times);
            var expected_index = 3;

            QAssert.IsTrue(tg.closestIndex(4) == expected_index,
                           "Expected index: " + expected_index + ", which does not match " +
                           "the returned index: " + tg.closestIndex(4));
        }

        [Fact]
        public void testClosestTime()
        {
            // Testing that the returned time matches the requested index
            var test_times = new List<double> { 1.0, 2.0, 5.0 };
            var tg = new TimeGrid(test_times);
            var expected_time = 5;

            QAssert.IsTrue(tg.closestTime(4).IsEqual(expected_time),
                           "Expected time of: " + expected_time + ", which does not match " +
                           "the returned time: " + tg.closestTime(4));
        }

        [Fact]
        public void testMandatoryTimes()
        {
            // Testing that mandatory times are recalled correctly
            var test_times = new List<double> { 1.0, 2.0, 4.0 };
            var tg = new TimeGrid(test_times, 8);

            // Mandatory times are those provided by the original iterator.
            var tg_mandatory_times = tg.mandatoryTimes();
            QAssert.CollectionAreEqual(tg_mandatory_times, test_times);
        }
    }
}
