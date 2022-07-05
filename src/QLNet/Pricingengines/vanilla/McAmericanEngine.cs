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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    //! American Monte Carlo engine
    /*! References:

        \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    [PublicAPI]
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

        protected override PathPricer<IPath> controlPathPricer()
        {
            var payoff = arguments_.payoff as StrikedTypePayoff;
            Utils.QL_REQUIRE(payoff != null, () => "StrikedTypePayoff needed for control variate");

            var process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            return new EuropeanPathPricer(payoff.optionType(), payoff.strike(),
                process.riskFreeRate().link.discount(timeGrid().Last()));
        }

        protected override IPricingEngine controlPricingEngine()
        {
            var process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            return new AnalyticEuropeanEngine(process);
        }

        protected override double? controlVariateValue()
        {
            var controlPE = controlPricingEngine();

            Utils.QL_REQUIRE(controlPE != null, () => "engine does not provide control variation pricing engine");

            var controlArguments = controlPE.getArguments() as QLNet.Option.Arguments;
            controlArguments = arguments_;
            controlArguments.exercise = new EuropeanExercise(arguments_.exercise.lastDate());

            controlPE.calculate();

            var controlResults = controlPE.getResults() as OneAssetOption.Results;

            return controlResults.value;
        }

        protected override LongstaffSchwartzPathPricer<IPath> lsmPathPricer()
        {
            var process = process_ as GeneralizedBlackScholesProcess;
            Utils.QL_REQUIRE(process != null, () => "generalized Black-Scholes process required");

            var exercise = arguments_.exercise as EarlyExercise;
            Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");
            Utils.QL_REQUIRE(!exercise.payoffAtExpiry(), () => "payoff at expiry not handled");

            var earlyExercisePathPricer = new AmericanPathPricer(arguments_.payoff, polynomOrder_, polynomType_);

            return new LongstaffSchwartzPathPricer<IPath>(timeGrid(), earlyExercisePathPricer, process.riskFreeRate());
        }
    }

    //! Monte Carlo American engine factory
    //template <class RNG = PseudoRandom, class S = Statistics>

    [PublicAPI]
    public class MakeMCAmericanEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        private bool antithetic_, controlVariate_;
        private int calibrationSamples_;
        private int polynomOrder_;
        private LsmBasisSystem.PolynomType polynomType_;
        private GeneralizedBlackScholesProcess process_;
        private int? samples_, maxSamples_;
        private ulong seed_;
        private int? steps_, stepsPerYear_;
        private double? tolerance_;

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

        // conversion to pricing engine
        public IPricingEngine value()
        {
            Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");

            return new MCAmericanEngine<RNG, S>(process_, steps_, stepsPerYear_, antithetic_, controlVariate_, samples_, tolerance_,
                maxSamples_, seed_, polynomOrder_, polynomType_, calibrationSamples_);
        }

        public MakeMCAmericanEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () => "chosen random generator policy does not allow an error estimate");

            tolerance_ = tolerance;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate() => withAntitheticVariate(true);

        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
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

        public MakeMCAmericanEngine<RNG, S> withControlVariate(bool b)
        {
            controlVariate_ = b;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withPolynomOrder(int polynomOrder)
        {
            polynomOrder_ = polynomOrder;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCAmericanEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
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
    }
}
