﻿/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math.RandomNumbers;
using QLNet.Methods.Finitedifferences.Utilities;

namespace QLNet.Math.Optimization
{
    /* The algorithm and strategy names are taken from here:

        Price, K., Storn, R., 1997. Differential Evolution -
        A Simple and Efficient Heuristic for Global Optimization
        over Continuous Spaces.
        Journal of Global Optimization, Kluwer Academic Publishers,
        1997, Vol. 11, pp. 341 - 359.

        There are seven basic strategies for creating mutant population
        currently implemented. Three basic crossover types are also
        available.

    */

    /// <summary>
    ///     Differential Evolution configuration object
    ///     OptimizationMethod using Differential Evolution algorithm
    /// </summary>
    [PublicAPI]
    public class DifferentialEvolution : OptimizationMethod
    {
        public enum CrossoverType
        {
            Normal,
            Binomial,
            Exponential
        }

        public enum Strategy
        {
            Rand1Standard,
            BestMemberWithJitter,
            CurrentToBest2Diffs,
            Rand1DiffWithPerVectorDither,
            Rand1DiffWithDither,
            EitherOrWithOptimalRecombination,
            Rand1SelfadaptiveWithRotation
        }

        [PublicAPI]
        public class Candidate : ICloneable
        {
            public Candidate(int size)
            {
                values = new Vector(size, 0.0);
                cost = 0.0;
            }

            public Candidate() : this(0)
            {
            }

            public double cost { get; set; }

            public Vector values { get; set; }

            public object Clone()
            {
                var c = new Candidate(values.size());
                values.ForEach((ii, vv) => c.values[ii] = vv);
                c.cost = cost;
                return c;
            }
        }

        [PublicAPI]
        public class Configuration
        {
            public Configuration()
            {
                strategy = Strategy.BestMemberWithJitter;
                crossoverType = CrossoverType.Normal;
                populationMembers = 100;
                stepsizeWeight = 0.2;
                crossoverProbability = 0.9;
                seed = 0;
                applyBounds = true;
                crossoverIsAdaptive = false;
            }

            public bool applyBounds { get; set; }

            public bool crossoverIsAdaptive { get; set; }

            public double crossoverProbability { get; set; }

            public CrossoverType crossoverType { get; set; }

            public int populationMembers { get; set; }

            public ulong seed { get; set; }

            public double stepsizeWeight { get; set; }

            public Strategy strategy { get; set; }

            public Configuration withAdaptiveCrossover(bool b = true)
            {
                crossoverIsAdaptive = b;
                return this;
            }

            public Configuration withBounds(bool b = true)
            {
                applyBounds = b;
                return this;
            }

            public Configuration withCrossoverProbability(double p)
            {
                QLNet.Utils.QL_REQUIRE(p >= 0.0 && p <= 1.0,
                    () => "Crossover probability (" + p
                                                    + ") must be in [0,1] range");
                crossoverProbability = p;
                return this;
            }

            public Configuration withCrossoverType(CrossoverType t)
            {
                crossoverType = t;
                return this;
            }

            public Configuration withPopulationMembers(int n)
            {
                QLNet.Utils.QL_REQUIRE(n > 0, () => "Positive number of population members required");
                populationMembers = n;
                return this;
            }

            public Configuration withSeed(ulong s)
            {
                seed = s;
                return this;
            }

            public Configuration withStepsizeWeight(double w)
            {
                QLNet.Utils.QL_REQUIRE(w >= 0 && w <= 2.0,
                    () => "Step size weight (" + w
                                               + ") must be in [0,2] range");
                stepsizeWeight = w;
                return this;
            }

            public Configuration withStrategy(Strategy s)
            {
                strategy = s;
                return this;
            }
        }

        [PublicAPI]
        public class sort_by_cost : IComparer<Candidate>
        {
            public int Compare(Candidate left, Candidate right)
            {
                if (left.cost < right.cost)
                {
                    return -1;
                }

                if (left.cost.IsEqual(right.cost))
                {
                    return 0;
                }

                return 1;
            }
        }

        protected Candidate bestMemberEver_;
        protected Configuration configuration_;
        protected Vector currGenSizeWeights_, currGenCrossover_;
        protected MersenneTwisterUniformRng rng_;
        protected Vector upperBound_, lowerBound_;

        public DifferentialEvolution(Configuration configuration = null)
        {
            configuration_ = configuration ?? new Configuration();
            rng_ = new MersenneTwisterUniformRng(configuration_.seed);
        }

        public Configuration configuration() => configuration_;

        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
        {
            var ecType = EndCriteria.Type.None;

            upperBound_ = P.constraint().upperBound(P.currentValue());
            lowerBound_ = P.constraint().lowerBound(P.currentValue());
            currGenSizeWeights_ = new Vector(configuration().populationMembers,
                configuration().stepsizeWeight);
            currGenCrossover_ = new Vector(configuration().populationMembers,
                configuration().crossoverProbability);

            List<Candidate> population = new InitializedList<Candidate>(configuration().populationMembers);
            population.ForEach((ii, vv) => population[ii] = new Candidate(P.currentValue().size()));

            fillInitialPopulation(population, P);

            //original quantlib use partial_sort as only first elements is needed
            var fxOld = population.Min(x => x.cost);
            bestMemberEver_ = (Candidate)population.First(x => x.cost.IsEqual(fxOld)).Clone();
            int iteration = 0, stationaryPointIteration = 0;

            // main loop - calculate consecutive emerging populations
            while (!endCriteria.checkMaxIterations(iteration++, ref ecType))
            {
                calculateNextGeneration(population, P.costFunction());

                var fxNew = population.Min(x => x.cost);
                var tmp = (Candidate)population.First(x => x.cost.IsEqual(fxNew)).Clone();

                if (fxNew < bestMemberEver_.cost)
                {
                    bestMemberEver_ = tmp;
                }

                if (endCriteria.checkStationaryFunctionValue(fxOld, fxNew, ref stationaryPointIteration,
                        ref ecType))
                {
                    break;
                }

                fxOld = fxNew;
            }

            P.setCurrentValue(bestMemberEver_.values);
            P.setFunctionValue(bestMemberEver_.cost);
            return ecType;
        }

        protected void adaptCrossover()
        {
            var crossoverChangeProb = 0.1; // [=tau2]
            for (var coIter = 0; coIter < currGenCrossover_.size(); coIter++)
            {
                if (rng_.nextReal() < crossoverChangeProb)
                {
                    currGenCrossover_[coIter] = rng_.nextReal();
                }
            }
        }

        protected void adaptSizeWeights()
        {
            // [=Fl & =Fu] respectively see Brest, J. et al., 2006,
            // "Self-Adapting Control Parameters in Differential
            // Evolution"
            double sizeWeightLowerBound = 0.1, sizeWeightUpperBound = 0.9;
            // [=tau1] A Comparative Study on Numerical Benchmark
            // Problems." page 649 for reference
            var sizeWeightChangeProb = 0.1;
            for (var coIter = 0; coIter < currGenSizeWeights_.size(); coIter++)
            {
                if (rng_.nextReal() < sizeWeightChangeProb)
                {
                    currGenSizeWeights_[coIter] = sizeWeightLowerBound + rng_.nextReal() * sizeWeightUpperBound;
                }
            }
        }

        protected void calculateNextGeneration(List<Candidate> population,
            CostFunction costFunction)
        {
            List<Candidate> mirrorPopulation = null;
            var oldPopulation = (List<Candidate>)population.Clone();

            switch (configuration().strategy)
            {
                case Strategy.Rand1Standard:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var shuffledPop2 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();

                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        population[popIter].values = population[popIter].values
                                                     + configuration().stepsizeWeight
                                                     * (shuffledPop1[popIter].values - shuffledPop2[popIter].values);
                    }
                }
                    break;

                case Strategy.BestMemberWithJitter:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var jitter = new Vector(population[0].values.size(), 0.0);

                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        for (var jitterIter = 0; jitterIter < jitter.Count; jitterIter++)
                        {
                            jitter[jitterIter] = rng_.nextReal();
                        }

                        population[popIter].values = bestMemberEver_.values
                                                     + Vector.DirectMultiply(
                                                         shuffledPop1[popIter].values - population[popIter].values
                                                         , 0.0001 * jitter + configuration().stepsizeWeight);
                    }

                    mirrorPopulation = new InitializedList<Candidate>(population.Count);
                    mirrorPopulation.ForEach((ii, vv) => mirrorPopulation[ii] = (Candidate)bestMemberEver_.Clone());
                }
                    break;

                case Strategy.CurrentToBest2Diffs:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();

                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        population[popIter].values = oldPopulation[popIter].values
                                                     + configuration().stepsizeWeight
                                                     * (bestMemberEver_.values - oldPopulation[popIter].values)
                                                     + configuration().stepsizeWeight
                                                     * (population[popIter].values - shuffledPop1[popIter].values);
                    }

                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();
                }
                    break;

                case Strategy.Rand1DiffWithPerVectorDither:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var shuffledPop2 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();
                    var FWeight = new Vector(population.First().values.size(), 0.0);
                    for (var fwIter = 0; fwIter < FWeight.Count; fwIter++)
                    {
                        FWeight[fwIter] = (1.0 - configuration().stepsizeWeight)
                            * rng_.nextReal() + configuration().stepsizeWeight;
                    }

                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        population[popIter].values = population[popIter].values
                                                     + Vector.DirectMultiply(FWeight,
                                                         shuffledPop1[popIter].values - shuffledPop2[popIter].values);
                    }
                }
                    break;

                case Strategy.Rand1DiffWithDither:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var shuffledPop2 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();
                    var FWeight = (1.0 - configuration().stepsizeWeight) * rng_.nextReal()
                                  + configuration().stepsizeWeight;
                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        population[popIter].values = population[popIter].values
                                                     + FWeight * (shuffledPop1[popIter].values -
                                                                  shuffledPop2[popIter].values);
                    }
                }
                    break;

                case Strategy.EitherOrWithOptimalRecombination:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var shuffledPop2 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();
                    var probFWeight = 0.5;
                    if (rng_.nextReal() < probFWeight)
                    {
                        for (var popIter = 0; popIter < population.Count; popIter++)
                        {
                            population[popIter].values = oldPopulation[popIter].values
                                                         + configuration().stepsizeWeight
                                                         * (shuffledPop1[popIter].values - shuffledPop2[popIter].values);
                        }
                    }
                    else
                    {
                        var K = 0.5 * (configuration().stepsizeWeight + 1); // invariant with respect to probFWeight used
                        for (var popIter = 0; popIter < population.Count; popIter++)
                        {
                            population[popIter].values = oldPopulation[popIter].values
                                                         + K
                                                         * (shuffledPop1[popIter].values - shuffledPop2[popIter].values
                                                                                         - 2.0 * population[popIter].values);
                        }
                    }
                }
                    break;

                case Strategy.Rand1SelfadaptiveWithRotation:
                {
                    population.Shuffle();
                    var shuffledPop1 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    var shuffledPop2 = (List<Candidate>)population.Clone();
                    population.Shuffle();
                    mirrorPopulation = (List<Candidate>)shuffledPop1.Clone();

                    adaptSizeWeights();

                    for (var popIter = 0; popIter < population.Count; popIter++)
                    {
                        if (rng_.nextReal() < 0.1)
                        {
                            population[popIter].values = rotateArray(bestMemberEver_.values);
                        }
                        else
                        {
                            population[popIter].values = bestMemberEver_.values
                                                         + currGenSizeWeights_[popIter]
                                                         * (shuffledPop1[popIter].values - shuffledPop2[popIter].values);
                        }
                    }
                }
                    break;

                default:
                    QLNet.Utils.QL_FAIL("Unknown strategy ("
                                                 + Convert.ToInt32(configuration().strategy) + ")");
                    break;
            }

            // in order to avoid unnecessary copying we use the same population object for mutants
            crossover(oldPopulation, population, population, mirrorPopulation,
                costFunction);
        }

        protected void crossover(List<Candidate> oldPopulation,
            List<Candidate> population,
            List<Candidate> mutantPopulation,
            List<Candidate> mirrorPopulation,
            CostFunction costFunction)
        {
            if (configuration().crossoverIsAdaptive)
            {
                adaptCrossover();
            }

            var mutationProbabilities = getMutationProbabilities(population);

            List<Vector> crossoverMask = new InitializedList<Vector>(population.Count);
            crossoverMask.ForEach((ii, vv) => crossoverMask[ii] = new Vector(population.First().values.size(), 1.0));

            List<Vector> invCrossoverMask = new InitializedList<Vector>(population.Count);
            invCrossoverMask.ForEach((ii, vv) => invCrossoverMask[ii] = new Vector(population.First().values.size(), 1.0));

            getCrossoverMask(crossoverMask, invCrossoverMask, mutationProbabilities);

            // crossover of the old and mutant population
            for (var popIter = 0; popIter < population.Count; popIter++)
            {
                population[popIter].values = Vector.DirectMultiply(oldPopulation[popIter].values, invCrossoverMask[popIter])
                                             + Vector.DirectMultiply(mutantPopulation[popIter].values,
                                                 crossoverMask[popIter]);
                // immediately apply bounds if specified
                if (configuration().applyBounds)
                {
                    for (var memIter = 0; memIter < population[popIter].values.size(); memIter++)
                    {
                        if (population[popIter].values[memIter] > upperBound_[memIter])
                        {
                            population[popIter].values[memIter] = upperBound_[memIter]
                                                                  + rng_.nextReal()
                                                                  * (mirrorPopulation[popIter].values[memIter]
                                                                     - upperBound_[memIter]);
                        }

                        if (population[popIter].values[memIter] < lowerBound_[memIter])
                        {
                            population[popIter].values[memIter] = lowerBound_[memIter]
                                                                  + rng_.nextReal()
                                                                  * (mirrorPopulation[popIter].values[memIter]
                                                                     - lowerBound_[memIter]);
                        }
                    }
                }

                // evaluate objective function as soon as possible to avoid unnecessary loops
                try
                {
                    population[popIter].cost = costFunction.value(population[popIter].values);

                    if (double.IsNaN(population[popIter].cost))
                    {
                        population[popIter].cost = double.MaxValue;
                    }
                }
                catch
                {
                    population[popIter].cost = double.MaxValue;
                }
            }
        }

        protected void fillInitialPopulation(List<Candidate> population, Problem p)
        {
            // use initial values provided by the user
            population.First().values = p.currentValue().Clone();
            population.First().cost = p.costFunction().value(population.First().values);

            if (double.IsNaN(population.First().cost))
            {
                population.First().cost = double.MaxValue;
            }

            // rest of the initial population is random
            for (var j = 1; j < population.Count; ++j)
            {
                for (var i = 0; i < p.currentValue().size(); ++i)
                {
                    double l = lowerBound_[i], u = upperBound_[i];
                    population[j].values[i] = l + (u - l) * rng_.nextReal();
                }

                population[j].cost = p.costFunction().value(population[j].values);

                if (double.IsNaN(population[j].cost))
                {
                    population[j].cost = double.MaxValue;
                }
            }
        }

        protected void getCrossoverMask(List<Vector> crossoverMask,
            List<Vector> invCrossoverMask,
            Vector mutationProbabilities)
        {
            for (var cmIter = 0; cmIter < crossoverMask.Count; cmIter++)
            {
                for (var memIter = 0; memIter < crossoverMask[cmIter].size(); memIter++)
                {
                    if (rng_.nextReal() < mutationProbabilities[cmIter])
                    {
                        invCrossoverMask[cmIter][memIter] = 0.0;
                    }
                    else
                    {
                        crossoverMask[cmIter][memIter] = 0.0;
                    }
                }
            }
        }

        protected Vector getMutationProbabilities(
            List<Candidate> population)
        {
            var mutationProbabilities = currGenCrossover_.Clone();

            switch (configuration().crossoverType)
            {
                case CrossoverType.Normal:
                    break;
                case CrossoverType.Binomial:
                    mutationProbabilities = currGenCrossover_
                                            * (1.0 - 1.0 / population.First().values.size())
                                            + 1.0 / population.First().values.size();
                    break;
                case CrossoverType.Exponential:
                    for (var coIter = 0; coIter < currGenCrossover_.size(); coIter++)
                    {
                        mutationProbabilities[coIter] =
                            (1.0 - System.Math.Pow(currGenCrossover_[coIter],
                                population.First().values.size()))
                            / (population.First().values.size()
                               * (1.0 - currGenCrossover_[coIter]));
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("Unknown crossover ExerciseType ("
                                                 + Convert.ToInt32(configuration().crossoverType) + ")");
                    break;
            }

            return mutationProbabilities;
        }

        protected Vector rotateArray(Vector inputVector)
        {
            var shuffle = inputVector.Clone();
            shuffle.Shuffle();
            return shuffle;
        }
    }
}
