/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.processes;
using QLNet.Time;

namespace QLNet.Pricingengines.vanilla
{
    //! Finite-differences pricing engine for BSM one asset options
    /*! The name is a misnomer as this is a base class for any finite difference scheme.  Its main job is to handle grid layout.

        \ingroup vanillaengines
    */
    [PublicAPI]
    public class FDVanillaEngine
    {
        // temporaries
        private const double safetyZoneFactor_ = 1.1;
        protected List<BoundaryCondition<IOperator>> BCs_;
        protected Date exerciseDate_;
        protected TridiagonalOperator finiteDifferenceOperator_;
        protected Payoff payoff_;
        protected GeneralizedBlackScholesProcess process_;
        // temporaries
        protected double sMin_, center_, sMax_;
        protected bool timeDependent_;
        protected int timeSteps_, gridPoints_;

        // required for generics and template iheritance
        public FDVanillaEngine()
        {
        }

        public FDVanillaEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
        {
            process_ = process;
            timeSteps_ = timeSteps;
            gridPoints_ = gridPoints;
            timeDependent_ = timeDependent;
            intrinsicValues_ = new SampledCurve(gridPoints);
            BCs_ = new InitializedList<BoundaryCondition<IOperator>>(2);
        }

        public SampledCurve intrinsicValues_ { get; set; }

        public virtual void calculate(IPricingEngineResults r)
        {
            throw new NotSupportedException();
        }

        public void ensureStrikeInGrid()
        {
            // ensure strike is included in the grid
            if (!(payoff_ is StrikedTypePayoff striked_payoff))
            {
                return;
            }

            var requiredGridValue = striked_payoff.strike();

            if (sMin_ > requiredGridValue / safetyZoneFactor_)
            {
                sMin_ = requiredGridValue / safetyZoneFactor_;
                // enforce central placement of the underlying
                sMax_ = center_ / (sMin_ / center_);
            }

            if (sMax_ < requiredGridValue * safetyZoneFactor_)
            {
                sMax_ = requiredGridValue * safetyZoneFactor_;
                // enforce central placement of the underlying
                sMin_ = center_ / (sMax_ / center_);
            }
        }

        // this should be defined as new in each deriving class which use template iheritance
        // in order to return a proper class to wrap
        public virtual FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent) =>
            new FDVanillaEngine(process, timeSteps, gridPoints, timeDependent);

        public double getResidualTime() => process_.time(exerciseDate_);

        public Vector grid() => intrinsicValues_.grid();

        public virtual void setupArguments(IPricingEngineArguments a)
        {
            var args = a as QLNet.Option.Arguments;
            Utils.QL_REQUIRE(args != null, () => "incorrect argument ExerciseType");

            exerciseDate_ = args.exercise.lastDate();
            payoff_ = args.payoff;
        }

        protected void initializeBoundaryConditions()
        {
            BCs_[0] = new NeumannBC(intrinsicValues_.value(1) - intrinsicValues_.value(0), BoundaryCondition<IOperator>.Side.Lower);
            BCs_[1] = new NeumannBC(intrinsicValues_.value(intrinsicValues_.size() - 1) -
                                    intrinsicValues_.value(intrinsicValues_.size() - 2),
                BoundaryCondition<IOperator>.Side.Upper);
        }

        protected void initializeInitialCondition()
        {
            intrinsicValues_.setLogGrid(sMin_, sMax_);
            intrinsicValues_.sample(payoff_.value);
        }

        protected void initializeOperator()
        {
            finiteDifferenceOperator_ = OperatorFactory.getOperator(process_, intrinsicValues_.grid(),
                getResidualTime(), timeDependent_);
        }

        protected virtual void setGridLimits()
        {
            setGridLimits(process_.stateVariable().link.value(), getResidualTime());
            ensureStrikeInGrid();
        }

        protected void setGridLimits(double center, double t)
        {
            Utils.QL_REQUIRE(center > 0.0, () => "negative or null underlying given");
            Utils.QL_REQUIRE(t > 0.0, () => "negative or zero residual time");
            center_ = center;
            var newGridPoints = safeGridPoints(gridPoints_, t);
            if (newGridPoints > intrinsicValues_.size())
            {
                intrinsicValues_ = new SampledCurve(newGridPoints);
            }

            var volSqrtTime = System.Math.Sqrt(process_.blackVolatility().link.blackVariance(t, center_));

            // the prefactor fine tunes performance at small volatilities
            var prefactor = 1.0 + 0.02 / volSqrtTime;
            var minMaxFactor = System.Math.Exp(4.0 * prefactor * volSqrtTime);
            sMin_ = center_ / minMaxFactor; // underlying grid min value
            sMax_ = center_ * minMaxFactor; // underlying grid max value
        }

        // safety check to be sure we have enough grid points.
        private int safeGridPoints(int gridPoints, double residualTime)
        {
            const int minGridPoints = 10;
            const int minGridPointsPerYear = 2;
            return System.Math.Max(gridPoints,
                residualTime > 1
                    ? (int)(minGridPoints + (residualTime - 1.0) * minGridPointsPerYear)
                    : minGridPoints);
        }
    }

    // this is the interface to allow generic use of FDAmericanEngine and FDShoutEngine
    // those engines are shortcuts to FDEngineAdapter
}
