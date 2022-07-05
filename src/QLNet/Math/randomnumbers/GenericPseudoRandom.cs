/*
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
using QLNet.Patterns;

namespace QLNet.Math.randomnumbers
{
    // random number traits
    [PublicAPI]
    public class GenericPseudoRandom<URNG, IC> : IRSG where URNG : IRNGTraits, new() where IC : IValue, new()
    {
        // data

        public static IC icInstance { get; set; } = FastActivator<IC>.Create();

        // more traits
        public int allowsErrorEstimate => 1;

        // factory
        public IRNG make_sequence_generator(int dimension, ulong seed)
        {
            var g = new RandomSequenceGenerator<URNG>(dimension, seed);
            return icInstance != null
                ? new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g, icInstance)
                : new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g);
        }
    }

    //! default traits for pseudo-random number generation
    /*! \test a sequence generator is generated and tested by comparing samples against known good values. */

    //! traits for Poisson-distributed pseudo-random number generation
    /*! \test sequence generators are generated and tested by comparing
              samples against known good values.
    */

    //! default traits for low-discrepancy sequence generation
}
