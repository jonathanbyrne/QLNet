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
using System;
using System.Collections.Generic;
using Xunit;
using QLNet.Math.Distributions;
using QLNet.Math.RandomNumbers;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_RNGTraits
    {
        [Fact]
        public void testGaussian()
        {
            //("Testing Gaussian pseudo-random number generation...");

            var rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>)
                      new PseudoRandom().make_sequence_generator(100, 1234);

            var values = rsg.nextSequence().value;
            var sum = 0.0;
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            var stored = 4.09916;
            var tolerance = 1.0e-5;
            if (System.Math.Abs(sum - stored) > tolerance)
            {
                QAssert.Fail("the sum of the samples does not match the stored value\n"
                             + "    calculated: " + sum + "\n"
                             + "    expected:   " + stored);
            }
        }

        [Fact]
        public void testDefaultPoisson()
        {

            //("Testing Poisson pseudo-random number generation...");

            PoissonPseudoRandom.icInstance = new InverseCumulativePoisson();
            var rsg = new PoissonPseudoRandom().make_sequence_generator(100, 1234);

            var values = rsg.nextSequence().value;
            var sum = 0.0;
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            var stored = 108.0;
            if (!Math.Utils.close(sum, stored))
            {
                QAssert.Fail("the sum of the samples does not match the stored value\n"
                             + "    calculated: " + sum + "\n"
                             + "    expected:   " + stored);
            }
        }

        [Fact]
        public void testCustomPoisson()
        {

            //("Testing custom Poisson pseudo-random number generation...");

            PoissonPseudoRandom.icInstance = new InverseCumulativePoisson(4.0);
            var rsg = new PoissonPseudoRandom().make_sequence_generator(100, 1234);

            var values = rsg.nextSequence().value;
            var sum = 0.0;
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            var stored = 409.0;
            if (!Math.Utils.close(sum, stored))
            {
                QAssert.Fail("the sum of the samples does not match the stored value\n"
                             + "    calculated: " + sum + "\n"
                             + "    expected:   " + stored);
            }
        }
    }
}
