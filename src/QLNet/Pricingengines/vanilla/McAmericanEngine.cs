﻿/*
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
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.Pricingengines;
using QLNet.processes;
using System;
using System.Collections.Generic;

namespace QLNet.Pricingengines.vanilla
{
    //! American Monte Carlo engine
    /*! References:

        \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    public class MCAmericanEngine<RNG, S> : MCLongstaffSchwartzEngine<OneAssetOption.Engine, SingleVariate, RNG, S>
      where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
    {

        private int polynomOrder_;
        private LsmBasisSystem.PolynomType polynomType_;

        //     int nCalibrationSamples = Null<Size>())
        public MCAmericanEngine(GeneralizedBlackScholesProcess process,
                                int? timeSteps,
                                int? timeStepsPerYear,
                                bool antitheticVariate,
                                bool controlVariate,
                                int? requiredSamples,
                                double? requiredTolerance,
                                int? maxSamples,
                                ulong seed,
                                int polynomOrder,
                                LsmBasisSystem.PolynomType polynomType,
                                int nCalibrationSamples)
        : base(process, timeSteps, timeStepsPerYear, false, antitheticVariate, controlVariate, requiredSamples,
               requiredTolerance, maxSamples, seed, nCalibrationSamples)
        {
            polynomOrder_ = polynomOrder;
            polynomType_ = polynomType;
        }


        public override void calculate()
        {
            base.calculate();
            if (controlVariate_)
            {
                // control variate might lead to small negative
                // option values for deep OTM options
                results_.value = System.Math.Max(0.0, results_.value.Value);
            }
        }

        protected override LongstaffSchwartzPathPricer<IPath> lsmPathPricer()
        {
            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            EarlyExercise exercise = arguments_.exercise as EarlyExercise;
            Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");
            Utils.QL_REQUIRE(!exercise.payoffAtExpiry(), () => "payoff at expiry not handled");

            AmericanPathPricer earlyExercisePathPricer = new AmericanPathPricer(arguments_.payoff, polynomOrder_, polynomType_);

            return new LongstaffSchwartzPathPricer<IPath>(timeGrid(), earlyExercisePathPricer, process.riskFreeRate());
        }

        protected override double? controlVariateValue()
        {
            IPricingEngine controlPE = controlPricingEngine();

            Utils.QL_REQUIRE(controlPE != null, () => "engine does not provide control variation pricing engine");

            QLNet.Option.Arguments controlArguments = controlPE.getArguments() as QLNet.Option.Arguments;
            controlArguments = arguments_;
            controlArguments.exercise = new EuropeanExercise(arguments_.exercise.lastDate());

            controlPE.calculate();

            OneAssetOption.Results controlResults = controlPE.getResults() as OneAssetOption.Results;

            return controlResults.value;
        }

        protected override IPricingEngine controlPricingEngine()
        {
            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            return new AnalyticEuropeanEngine(process);
        }

        protected override PathPricer<IPath> controlPathPricer()
        {
            StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
            Utils.QL_REQUIRE(payoff != null, () => "StrikedTypePayoff needed for control variate");

            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            return new EuropeanPathPricer(payoff.optionType(), payoff.strike(),
                                          process.riskFreeRate().link.discount(timeGrid().Last()));
        }
    }


    public class AmericanPathPricer : IEarlyExercisePathPricer<IPath, double>
    {
        protected double scalingValue_;
        protected Payoff payoff_;
        protected List<Func<double, double>> v_ = new List<Func<double, double>>();

        public AmericanPathPricer(Payoff payoff, int polynomOrder, LsmBasisSystem.PolynomType polynomType)
        {
            scalingValue_ = 1;
            payoff_ = payoff;
            v_ = LsmBasisSystem.pathBasisSystem(polynomOrder, polynomType);

            Utils.QL_REQUIRE(polynomType == LsmBasisSystem.PolynomType.Monomial
                              || polynomType == LsmBasisSystem.PolynomType.Laguerre
                              || polynomType == LsmBasisSystem.PolynomType.Hermite
                              || polynomType == LsmBasisSystem.PolynomType.Hyperbolic
                              || polynomType == LsmBasisSystem.PolynomType.Chebyshev2th, () => "insufficient polynom type");

            // the payoff gives an additional value
            v_.Add(this.payoff);

            StrikedTypePayoff strikePayoff = payoff_ as StrikedTypePayoff;

            if (strikePayoff != null)
            {
                scalingValue_ /= strikePayoff.strike();
            }
        }

        // scale values of the underlying to increase numerical stability
        public double state(IPath path, int t) { return (path as Path)[t] * scalingValue_; }
        public double value(IPath path, int t) { return payoff(state(path, t)); }
        public List<Func<double, double>> basisSystem() { return v_; }
        protected double payoff(double state) { return payoff_.value(state / scalingValue_); }
    }


    //! Monte Carlo American engine factory
    //template <class RNG = PseudoRandom, class S = Statistics>
    public class MakeMCAmericanEngine<RNG> : MakeMCAmericanEngine<RNG, Statistics>
       where RNG : IRSG, new()
    {
        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process) : base(process) { }
    }

    public class MakeMCAmericanEngine<RNG, S>
       where RNG : IRSG, new()
       where S : IGeneralStatistics, new()
    {

        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_, controlVariate_;
        private int? steps_, stepsPerYear_;
        private int? samples_, maxSamples_;
        private int calibrationSamples_;
        private double? tolerance_;
        private ulong seed_;
        private int polynomOrder_;
        private LsmBasisSystem.PolynomType polynomType_;

        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            controlVariate_ = false;
            steps_ = null;
            stepsPerYear_ = null;
            samples_ = null;
            maxSamples_ = null;
            calibrationSamples_ = 2048;
            tolerance_ = null;
            seed_ = 0;
            polynomOrder_ = 2;
            polynomType_ = LsmBasisSystem.PolynomType.Monomial;
        }

        // named parameters
        public MakeMCAmericanEngine<RNG, S> withSteps(int steps)
        {
            steps_ = steps;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withStepsPerYear(int steps)
        {
            stepsPerYear_ = steps;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () => "chosen random generator policy does not allow an error estimate");

            tolerance_ = tolerance;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate() { return withAntitheticVariate(true); }
        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withControlVariate(bool b)
        {
            controlVariate_ = b;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withPolynomOrder(int polynomOrder)
        {
            polynomOrder_ = polynomOrder;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withBasisSystem(LsmBasisSystem.PolynomType polynomType)
        {
            polynomType_ = polynomType;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withCalibrationSamples(int samples)
        {
            calibrationSamples_ = samples;
            return this;
        }

        // conversion to pricing engine
        public IPricingEngine value()
        {
            Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");

            return new MCAmericanEngine<RNG, S>(process_, steps_, stepsPerYear_, antithetic_, controlVariate_, samples_, tolerance_,
                                                maxSamples_, seed_, polynomOrder_, polynomType_, calibrationSamples_);
        }
    }
}
