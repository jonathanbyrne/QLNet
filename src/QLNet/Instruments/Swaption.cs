﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using JetBrains.Annotations;
using QLNet.Math.Solvers1d;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Instruments
{
    //! %settlement information

    //! %Swaption class
    /*! \ingroup instruments

        \test
        - the correctness of the returned value is tested by checking
          that the price of a payer (resp. receiver) swaption
          decreases (resp. increases) with the strike.
        - the correctness of the returned value is tested by checking
          that the price of a payer (resp. receiver) swaption
          increases (resp. decreases) with the spread.
        - the correctness of the returned value is tested by checking
          it against that of a swaption on a swap with no spread and a
          correspondingly adjusted fixed rate.
        - the correctness of the returned value is tested by checking
          it against a known good value.
        - the correctness of the returned value of cash settled swaptions
          is tested by checking the modified annuity against a value
          calculated without using the Swaption class.


        \todo add greeks and explicit exercise lag
    */

    [PublicAPI]
    public class Swaption : Option
    {
        // arguments, pricing engine
        public new class Arguments : VanillaSwap.Arguments
        {
            public Arguments()
            {
                settlementType = Settlement.Type.Physical;
            }

            public Exercise exercise { get; set; }

            public Settlement.Method settlementMethod { get; set; }

            public Settlement.Type settlementType { get; set; }

            public VanillaSwap swap { get; set; }
        }

        private Settlement.Method settlementMethod_;
        private Settlement.Type settlementType_;

        // arguments
        private VanillaSwap swap_;

        public Swaption(VanillaSwap swap,
            Exercise exercise,
            Settlement.Type delivery = Settlement.Type.Physical,
            Settlement.Method settlementMethod = Settlement.Method.PhysicalOTC)
            : base(new Payoff(), exercise)
        {
            settlementType_ = delivery;
            settlementMethod_ = settlementMethod;
            swap_ = swap;
            swap_.registerWith(update);
        }

        public Arguments arguments { get; set; }

        public SwaptionEngine engine { get; set; }

        //! implied volatility
        public double impliedVolatility(double targetValue,
            Handle<YieldTermStructure> discountCurve,
            double guess,
            double accuracy = 1.0e-4,
            int maxEvaluations = 100,
            double minVol = 1.0e-7,
            double maxVol = 4.0,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double? displacement = 0.0)
        {
            calculate();
            if (isExpired())
            {
                throw new ArgumentException("instrument expired");
            }

            var f = new ImpliedVolHelper_(this, discountCurve, targetValue, displacement, type);
            var solver = new NewtonSafe();
            solver.setMaxEvaluations(maxEvaluations);
            return solver.solve(f, accuracy, guess, minVol, maxVol);
        }

        // Instrument interface
        public override bool isExpired() => new simple_event(exercise_.dates().Last()).hasOccurred();

        public Settlement.Method settlementMethod() => settlementMethod_;

        // Inspectors
        public Settlement.Type settlementType() => settlementType_;

        public override void setupArguments(IPricingEngineArguments args)
        {
            swap_.setupArguments(args);

            if (!(args is Arguments arguments))
            {
                throw new ArgumentException("wrong argument ExerciseType");
            }

            arguments.swap = swap_;
            arguments.settlementType = settlementType_;
            arguments.settlementMethod = settlementMethod_;
            arguments.exercise = exercise_;
        }

        public VanillaSwap.Type type() => swap_.swapType;

        public VanillaSwap underlyingSwap() => swap_;

        public void validate()
        {
            arguments.validate();
            if (arguments.swap == null)
            {
                throw new ArgumentException("vanilla swap not set");
            }

            if (arguments.exercise == null)
            {
                throw new ArgumentException("exercise not set");
            }

            Settlement.checkTypeAndMethodConsistency(arguments.settlementType,
                arguments.settlementMethod);
        }
    }

    //! base class for swaption engines
}
