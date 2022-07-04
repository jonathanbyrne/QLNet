﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using QLNet.processes;
using System;

namespace QLNet.Pricingengines.asian
{
    /// <summary>
    /// Analytic engine for discrete geometric average price Asian
    /// </summary>
    /// <remarks>
    /// This class implements a discrete geometric average price Asian
    /// option, with European exercise.  The formula is from "Asian
    /// Option", E. Levy (1997) in "Exotic Options: The State of the
    /// Art", edited by L. Clewlow, C. Strickland, pag 65-97
    /// </remarks>
    /// <typeparam name="RNG"></typeparam>
    /// <typeparam name="S"></typeparam>
    [JetBrains.Annotations.PublicAPI] public class MCDiscreteGeometricAPEngine<RNG, S>
       : MCDiscreteAveragingAsianEngine<RNG, S>
         where RNG : IRSG, new()
         where S : IGeneralStatistics, new()
    {
        public MCDiscreteGeometricAPEngine(
           GeneralizedBlackScholesProcess process,
           int maxTimeStepPerYear,
           bool brownianBridge,
           bool antitheticVariate,
           bool controlVariate,
           int requiredSamples,
           double requiredTolerance,
           int maxSamples,
           ulong seed)
           : base(process, maxTimeStepPerYear, brownianBridge, antitheticVariate,
                  controlVariate, requiredSamples, requiredTolerance, maxSamples, seed)
        { }

        // conversion to pricing engine
        protected override PathPricer<IPath> pathPricer()
        {
            var payoff = (PlainVanillaPayoff)arguments_.payoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var exercise = (EuropeanExercise)arguments_.exercise;
            Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

            return (PathPricer<IPath>)new GeometricAPOPathPricer(
                      payoff.optionType(),
                      payoff.strike(),
                      process_.riskFreeRate().link.discount(
                         timeGrid().Last()),
                      arguments_.runningAccumulator.GetValueOrDefault(),
                      arguments_.pastFixings.GetValueOrDefault());
        }
    }

    [JetBrains.Annotations.PublicAPI] public class GeometricAPOPathPricer : PathPricer<Path>
    {
        private PlainVanillaPayoff payoff_;
        private double discount_;
        private double runningProduct_;
        private int pastFixings_;

        public GeometricAPOPathPricer(QLNet.Option.Type type,
                                      double strike,
                                      double discount,
                                      double runningProduct,
                                      int pastFixings)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;
            runningProduct_ = runningProduct;
            pastFixings_ = pastFixings;
            Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
        }

        public GeometricAPOPathPricer(QLNet.Option.Type type,
                                      double strike,
                                      double discount,
                                      double runningProduct)
           : this(type, strike, discount, runningProduct, 0)
        { }

        public GeometricAPOPathPricer(QLNet.Option.Type type,
                                      double strike,
                                      double discount)
           : this(type, strike, discount, 1.0, 0)
        { }

        public double value(Path path)
        {
            var n = path.length() - 1;
            Utils.QL_REQUIRE(n > 0, () => "the path cannot be empty");

            double averagePrice;
            var product = runningProduct_;
            var fixings = n + pastFixings_;
            if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
            {
                fixings += 1;
                product *= path.front();
            }
            // care must be taken not to overflow product
            var maxValue = double.MaxValue;
            averagePrice = 1.0;
            for (var i = 1; i < n + 1; i++)
            {
                var price = path[i];
                if (product < maxValue / price)
                {
                    product *= price;
                }
                else
                {
                    averagePrice *= System.Math.Pow(product, 1.0 / fixings);
                    product = price;
                }
            }
            averagePrice *= System.Math.Pow(product, 1.0 / fixings);
            return discount_ * payoff_.value(averagePrice);
        }
    }

    //<class RNG = PseudoRandom, class S = Statistics>
    [JetBrains.Annotations.PublicAPI] public class MakeMCDiscreteGeometricAPEngine<RNG, S>
       where RNG : IRSG, new()
       where S : Statistics, new()
    {
        public MakeMCDiscreteGeometricAPEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            controlVariate_ = false;
            steps_ = null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            brownianBridge_ = true;
            seed_ = 0;
        }

        // named parameters
        public MakeMCDiscreteGeometricAPEngine<RNG, S> withStepsPerYear(int maxSteps)
        {
            steps_ = maxSteps;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge(bool b)
        {
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge() => withBrownianBridge(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                             "chosen random generator policy " + "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate() => withAntitheticVariate(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate(bool b)
        {
            controlVariate_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate() => withControlVariate(true);

        // conversion to pricing engine
        public IPricingEngine value()
        {
            Utils.QL_REQUIRE(steps_ != null, () => "max number of steps per year not given");
            return new MCDiscreteGeometricAPEngine<RNG, S>(process_,
                                                                            steps_.Value,
                                                                            brownianBridge_,
                                                                            antithetic_, controlVariate_,
                                                                            samples_.Value, tolerance_.Value,
                                                                            maxSamples_.Value,
                                                                            seed_);
        }

        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_, controlVariate_;
        private int? steps_, samples_, maxSamples_;
        private double? tolerance_;
        private bool brownianBridge_;
        private ulong seed_;
    }
}

