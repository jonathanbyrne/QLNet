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

using QLNet.Math.Distributions;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;

namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public interface IRNGTraits
    {
        ulong nextInt32();
        Sample<double> next();

        IRNGTraits factory(ulong seed);
    }

    [JetBrains.Annotations.PublicAPI] public interface IRSG
    {
        int allowsErrorEstimate { get; }
        IRNG make_sequence_generator(int dimension, ulong seed);
    }

    // random number traits
    [JetBrains.Annotations.PublicAPI] public class GenericPseudoRandom<URNG, IC> : IRSG where URNG : IRNGTraits, new() where IC : IValue, new()
    {
        // data
        private static IC icInstance_ = FastActivator<IC>.Create();

        // more traits
        public int allowsErrorEstimate => 1;

        public static IC icInstance
        {
            get => icInstance_;
            set => icInstance_ = value;
        }

        // factory
        public IRNG make_sequence_generator(int dimension, ulong seed)
        {
            var g = new RandomSequenceGenerator<URNG>(dimension, seed);
            return icInstance_ != null
                    ? new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g, icInstance_)
                    : new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g);
        }
    }

    //! default traits for pseudo-random number generation
    /*! \test a sequence generator is generated and tested by comparing samples against known good values. */
    [JetBrains.Annotations.PublicAPI] public class PseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativeNormal> { }

    //! traits for Poisson-distributed pseudo-random number generation
    /*! \test sequence generators are generated and tested by comparing
              samples against known good values.
    */
    [JetBrains.Annotations.PublicAPI] public class PoissonPseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativePoisson> { }


    [JetBrains.Annotations.PublicAPI] public class GenericLowDiscrepancy<URSG, IC> : IRSG where URSG : IRNG, new() where IC : IValue, new()
    {
        // data
        private static IC icInstance_ = FastActivator<IC>.Create();
        public static IC icInstance
        {
            get => icInstance_;
            set => icInstance_ = value;
        }


        // more traits
        public int allowsErrorEstimate => 0;

        // factory
        public IRNG make_sequence_generator(int dimension, ulong seed)
        {
            var g = (URSG)FastActivator<URSG>.Create().factory(dimension, seed);
            return icInstance != null ? new InverseCumulativeRsg<URSG, IC>(g, icInstance)
                    : new InverseCumulativeRsg<URSG, IC>(g);
        }
    }

    //! default traits for low-discrepancy sequence generation
    [JetBrains.Annotations.PublicAPI] public class LowDiscrepancy : GenericLowDiscrepancy<SobolRsg, InverseCumulativeNormal> { }
}
