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
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QLNet.Math.Optimization;
using QLNet.Math;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Optimizers
    {
        List<CostFunction> costFunctions_ = new List<CostFunction>();
        List<Constraint> constraints_ = new List<Constraint>();
        List<Vector> initialValues_ = new List<Vector>();
        List<int> maxIterations_ = new List<int>(), maxStationaryStateIterations_ = new List<int>();
        List<double> rootEpsilons_ = new List<double>(),
        functionEpsilons_ = new List<double>(),
        gradientNormEpsilons_ = new List<double>();
        List<EndCriteria> endCriterias_ = new List<EndCriteria>();
        List<List<NamedOptimizationMethod>> optimizationMethods_ = new List<List<NamedOptimizationMethod>>();
        List<Vector> xMinExpected_ = new List<Vector>(), yMinExpected_ = new List<Vector>();

        struct NamedOptimizationMethod
        {
            public OptimizationMethod optimizationMethod;
            public string name;
        }

        enum OptimizationMethodType
        {
            simplex,
            levenbergMarquardt,
            levenbergMarquardt2,
            conjugateGradient,
            conjugateGradient_goldstein,
            steepestDescent,
            steepestDescent_goldstein,
            bfgs,
            bfgs_goldstein
        }

        [Fact]
        public void OptimizersTest()
        {
            //("Testing optimizers...");

            setup();

            // Loop over problems (currently there is only 1 problem)
            for (var i = 0; i < costFunctions_.Count; ++i)
            {
                var problem = new Problem(costFunctions_[i], constraints_[i], initialValues_[i]);
                var initialValues = problem.currentValue();
                // Loop over optimizers
                for (var j = 0; j < optimizationMethods_[i].Count; ++j)
                {
                    var rootEpsilon = endCriterias_[i].rootEpsilon();
                    var endCriteriaTests = 1;
                    // Loop over rootEpsilon
                    for (var k = 0; k < endCriteriaTests; ++k)
                    {
                        problem.setCurrentValue(initialValues);
                        var endCriteria = new EndCriteria(endCriterias_[i].maxIterations(),
                                                                  endCriterias_[i].maxStationaryStateIterations(),
                                                                  rootEpsilon,
                                                                  endCriterias_[i].functionEpsilon(),
                                                                  endCriterias_[i].gradientNormEpsilon());
                        rootEpsilon *= .1;
                        var endCriteriaResult =
                           optimizationMethods_[i][j].optimizationMethod.minimize(problem, endCriteria);
                        var xMinCalculated = problem.currentValue();
                        var yMinCalculated = problem.values(xMinCalculated);
                        // Check optimization results vs known solution
                        if (endCriteriaResult == EndCriteria.Type.None ||
                            endCriteriaResult == EndCriteria.Type.MaxIterations ||
                            endCriteriaResult == EndCriteria.Type.Unknown)
                            QAssert.Fail("function evaluations: " + problem.functionEvaluation() +
                                         " gradient evaluations: " + problem.gradientEvaluation() +
                                         " x expected:           " + xMinExpected_[i] +
                                         " x calculated:         " + xMinCalculated +
                                         " x difference:         " + (xMinExpected_[i] - xMinCalculated) +
                                         " rootEpsilon:          " + endCriteria.rootEpsilon() +
                                         " y expected:           " + yMinExpected_[i] +
                                         " y calculated:         " + yMinCalculated +
                                         " y difference:         " + (yMinExpected_[i] - yMinCalculated) +
                                         " functionEpsilon:      " + endCriteria.functionEpsilon() +
                                         " endCriteriaResult:    " + endCriteriaResult);
                    }
                }
            }
        }

        [Fact]
        public void nestedOptimizationTest()
        {
            //("Testing nested optimizations...");
            var optimizationBasedCostFunction = new OptimizationBasedCostFunction();
            var constraint = new NoConstraint();
            var initialValues = new Vector(1, 0.0);
            var problem = new Problem(optimizationBasedCostFunction, constraint, initialValues);
            var optimizationMethod = new LevenbergMarquardt();
            //Simplex optimizationMethod(0.1);
            //ConjugateGradient optimizationMethod;
            //SteepestDescent optimizationMethod;
            var endCriteria = new EndCriteria(1000, 100, 1e-5, 1e-5, 1e-5);
            optimizationMethod.minimize(problem, endCriteria);
        }

        [Fact]
        public void testDifferentialEvolution()
        {
            //BOOST_TEST_MESSAGE("Testing differential evolution...");

            /* Note:
            *
            * The "ModFourthDeJong" doesn't have a well defined optimum because
            * of its noisy part. It just has to be <= 15 in our example.
            * The concrete value might differ for a different input and
            * different random numbers.
            *
            * The "Griewangk" function is an example where the adaptive
            * version of DifferentialEvolution turns out to be more successful.
            */

            var conf =
               new DifferentialEvolution.Configuration()
            .withStepsizeWeight(0.4)
            .withBounds()
            .withCrossoverProbability(0.35)
            .withPopulationMembers(500)
            .withStrategy(DifferentialEvolution.Strategy.BestMemberWithJitter)
            .withCrossoverType(DifferentialEvolution.CrossoverType.Normal)
            .withAdaptiveCrossover()
            .withSeed(3242);

            var conf2 =
               new DifferentialEvolution.Configuration()
            .withStepsizeWeight(1.8)
            .withBounds()
            .withCrossoverProbability(0.9)
            .withPopulationMembers(1000)
            .withStrategy(DifferentialEvolution.Strategy.Rand1SelfadaptiveWithRotation)
            .withCrossoverType(DifferentialEvolution.CrossoverType.Normal)
            .withAdaptiveCrossover()
            .withSeed(3242);
            var deOptim2 = new DifferentialEvolution(conf2);

            var diffEvolOptimisers = new List<DifferentialEvolution>();
            diffEvolOptimisers.Add(new DifferentialEvolution(conf));
            diffEvolOptimisers.Add(new DifferentialEvolution(conf));
            diffEvolOptimisers.Add(new DifferentialEvolution(conf));
            diffEvolOptimisers.Add(new DifferentialEvolution(conf));
            diffEvolOptimisers.Add(deOptim2);

            var costFunctions = new List<CostFunction>();
            costFunctions.Add(new FirstDeJong());
            costFunctions.Add(new SecondDeJong());
            costFunctions.Add(new ModThirdDeJong());
            costFunctions.Add(new ModFourthDeJong());
            costFunctions.Add(new Griewangk());

            var constraints = new List<BoundaryConstraint>();
            constraints.Add(new BoundaryConstraint(-10.0, 10.0));
            constraints.Add(new BoundaryConstraint(-10.0, 10.0));
            constraints.Add(new BoundaryConstraint(-10.0, 10.0));
            constraints.Add(new BoundaryConstraint(-10.0, 10.0));
            constraints.Add(new BoundaryConstraint(-600.0, 600.0));

            var initialValues = new List<Vector>();
            initialValues.Add(new Vector(3, 5.0));
            initialValues.Add(new Vector(2, 5.0));
            initialValues.Add(new Vector(5, 5.0));
            initialValues.Add(new Vector(30, 5.0));
            initialValues.Add(new Vector(10, 100.0));

            var endCriteria = new List<EndCriteria>();
            endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
            endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
            endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
            endCriteria.Add(new EndCriteria(500, 100, 1e-10, 1e-8, null));
            endCriteria.Add(new EndCriteria(1000, 800, 1e-12, 1e-10, null));

            var minima = new List<double>();
            minima.Add(0.0);
            minima.Add(0.0);
            minima.Add(0.0);
            minima.Add(10.9639796558);
            minima.Add(0.0);

            for (var i = 0; i < costFunctions.Count; ++i)
            {
                var problem = new Problem(costFunctions[i], constraints[i], initialValues[i]);
                diffEvolOptimisers[i].minimize(problem, endCriteria[i]);

                if (i != 3)
                {
                    // stable
                    if (System.Math.Abs(problem.functionValue() - minima[i]) > 1e-8)
                    {
                        QAssert.Fail("costFunction # " + i
                                     + "\ncalculated: " + problem.functionValue()
                                     + "\nexpected:   " + minima[i]);
                    }
                }
                else
                {
                    // this case is unstable due to randomness; we're good as
                    // long as the result is below 15
                    if (problem.functionValue() > 15)
                    {
                        QAssert.Fail("costFunction # " + i
                                     + "\ncalculated: " + problem.functionValue()
                                     + "\nexpected:   " + "less than 15");
                    }
                }
            }
        }


        [Fact]
        public void testFunctionValueEqualsCostFunctionAtCurrentValue()
        {
            var testCostFunction = new TestCostFunction();
            var problem = new Problem(testCostFunction, new NoConstraint(), new Vector(new List<double> { 3, 7.4 }));
            var endCriteria = new EndCriteria(maxIterations: 1000, maxStationaryStateIterations: 10, rootEpsilon: 0, functionEpsilon: 1e-10, gradientNormEpsilon: null);
            var method = new BFGS();

            var endType = method.minimize(problem, endCriteria);
            QAssert.AreEqual(EndCriteria.Type.StationaryFunctionValue, endType);

            QAssert.AreEqual(problem.functionValue(), testCostFunction.value(problem.currentValue()));
        }

        private class TestCostFunction : CostFunction
        {
            public override Vector values(Vector x)
            {
                return new Vector(x.Select(z => z * z).ToList());
            }

            /// <inheritdoc />
            public override double value(Vector x)
            {
                return x.Sum(z => z * z);
            }

            /// <inheritdoc />
            public override void gradient(ref Vector grad, Vector x)
            {
                for (var i = 0; i < grad.Count; i++)
                    grad[i] = 2 * x[i];
            }
        }

        // Set up, for each cost function, all the ingredients for optimization:
        // constraint, initial guess, end criteria, optimization methods.
        void setup()
        {

            // Cost function n. 1: 1D polynomial of degree 2 (parabolic function y=a*x^2+b*x+c)
            const double a = 1;   // required a > 0
            const double b = 1;
            const double c = 1;
            var coefficients = new Vector() { c, b, a };

            costFunctions_.Add(new OneDimensionalPolynomialDegreeN(coefficients));
            // Set constraint for optimizers: unconstrained problem
            constraints_.Add(new NoConstraint());
            // Set initial guess for optimizer
            var initialValue = new Vector(1);
            initialValue[0] = -100;
            initialValues_.Add(initialValue);
            // Set end criteria for optimizer
            maxIterations_.Add(10000);                // maxIterations
            maxStationaryStateIterations_.Add(100);   // MaxStationaryStateIterations
            rootEpsilons_.Add(1e-8);                  // rootEpsilon
            functionEpsilons_.Add(1e-8);              // functionEpsilon
            gradientNormEpsilons_.Add(1e-8);          // gradientNormEpsilon
            endCriterias_.Add(new EndCriteria(maxIterations_.Last(), maxStationaryStateIterations_.Last(),
                                              rootEpsilons_.Last(), functionEpsilons_.Last(),
                                              gradientNormEpsilons_.Last()));

            // Set optimization methods for optimizer
            OptimizationMethodType[] optimizationMethodTypes =
            {
            OptimizationMethodType.simplex,
            OptimizationMethodType.levenbergMarquardt,
            OptimizationMethodType.levenbergMarquardt2,
            OptimizationMethodType.conjugateGradient/*, steepestDescent*/,
            OptimizationMethodType.conjugateGradient_goldstein,
            OptimizationMethodType.steepestDescent_goldstein,
            OptimizationMethodType.bfgs,
            OptimizationMethodType.bfgs_goldstein
         };

            var simplexLambda = 0.1;                   // characteristic search length for simplex
            var levenbergMarquardtEpsfcn = 1.0e-8;     // parameters specific for Levenberg-Marquardt
            var levenbergMarquardtXtol = 1.0e-8;     //
            var levenbergMarquardtGtol = 1.0e-8;     //
            optimizationMethods_.Add(makeOptimizationMethods(
                                        optimizationMethodTypes, optimizationMethodTypes.Length,
                                        simplexLambda, levenbergMarquardtEpsfcn, levenbergMarquardtXtol,
                                        levenbergMarquardtGtol));
            // Set expected results for optimizer
            Vector xMinExpected = new Vector(1), yMinExpected = new Vector(1);
            xMinExpected[0] = -b / (2.0 * a);
            yMinExpected[0] = -(b * b - 4.0 * a * c) / (4.0 * a);
            xMinExpected_.Add(xMinExpected);
            yMinExpected_.Add(yMinExpected);
        }


        OptimizationMethod makeOptimizationMethod(OptimizationMethodType optimizationMethodType,
                                                  double simplexLambda,
                                                  double levenbergMarquardtEpsfcn,
                                                  double levenbergMarquardtXtol,
                                                  double levenbergMarquardtGtol)
        {
            switch (optimizationMethodType)
            {
                case OptimizationMethodType.simplex:
                    return new Simplex(simplexLambda);
                case OptimizationMethodType.levenbergMarquardt:
                    return new LevenbergMarquardt(levenbergMarquardtEpsfcn, levenbergMarquardtXtol, levenbergMarquardtGtol);
                case OptimizationMethodType.levenbergMarquardt2:
                    return new LevenbergMarquardt(levenbergMarquardtEpsfcn, levenbergMarquardtXtol, levenbergMarquardtGtol, true);
                case OptimizationMethodType.conjugateGradient:
                    return new ConjugateGradient();
                case OptimizationMethodType.steepestDescent:
                    return new SteepestDescent();
                case OptimizationMethodType.bfgs:
                    return new BFGS();
                case OptimizationMethodType.conjugateGradient_goldstein:
                    return new ConjugateGradient(new GoldsteinLineSearch());
                case OptimizationMethodType.steepestDescent_goldstein:
                    return new SteepestDescent(new GoldsteinLineSearch());
                case OptimizationMethodType.bfgs_goldstein:
                    return new BFGS(new GoldsteinLineSearch());
                default:
                    throw new Exception("unknown OptimizationMethod ExerciseType");
            }
        }

        List<NamedOptimizationMethod> makeOptimizationMethods(OptimizationMethodType[] optimizationMethodTypes,
                                                              int optimizationMethodNb,
                                                              double simplexLambda,
                                                              double levenbergMarquardtEpsfcn,
                                                              double levenbergMarquardtXtol,
                                                              double levenbergMarquardtGtol)
        {
            var results = new List<NamedOptimizationMethod>(optimizationMethodNb);
            for (var i = 0; i < optimizationMethodNb; ++i)
            {
                NamedOptimizationMethod namedOptimizationMethod;
                namedOptimizationMethod.optimizationMethod = makeOptimizationMethod(optimizationMethodTypes[i],
                                                                                    simplexLambda,
                                                                                    levenbergMarquardtEpsfcn,
                                                                                    levenbergMarquardtXtol,
                                                                                    levenbergMarquardtGtol);
                namedOptimizationMethod.name = optimizationMethodTypeToString(optimizationMethodTypes[i]);
                results.Add(namedOptimizationMethod);
            }
            return results;
        }

        string optimizationMethodTypeToString(OptimizationMethodType type)
        {
            switch (type)
            {
                case OptimizationMethodType.simplex:
                    return "Simplex";
                case OptimizationMethodType.levenbergMarquardt:
                    return "Levenberg Marquardt";
                case OptimizationMethodType.levenbergMarquardt2:
                    return "Levenberg Marquardt (cost function's jacbobian)";
                case OptimizationMethodType.conjugateGradient:
                    return "Conjugate Gradient";
                case OptimizationMethodType.steepestDescent:
                    return "Steepest Descent";
                case OptimizationMethodType.bfgs:
                    return "BFGS";
                case OptimizationMethodType.conjugateGradient_goldstein:
                    return "Conjugate Gradient (Goldstein line search)";
                case OptimizationMethodType.steepestDescent_goldstein:
                    return "Steepest Descent (Goldstein line search)";
                case OptimizationMethodType.bfgs_goldstein:
                    return "BFGS (Goldstein line search)";
                default:
                    throw new Exception("unknown OptimizationMethod ExerciseType");
            }
        }
    }

    // The goal of this cost function is simply to call another optimization inside
    // in order to test nested optimizations
}
