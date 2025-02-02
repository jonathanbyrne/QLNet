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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! Finite-differences pricing engine for American-style vanilla options
    [PublicAPI]
    public class FDStepConditionEngine : FDConditionEngineTemplate
    {
        protected List<BoundaryCondition<IOperator>> controlBCs_;
        protected TridiagonalOperator controlOperator_;
        protected SampledCurve controlPrices_;

        // required for generics
        public FDStepConditionEngine()
        {
        }

        //public FDStepConditionEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints,
        //     bool timeDependent = false)
        public FDStepConditionEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            controlBCs_ = new InitializedList<BoundaryCondition<IOperator>>(2);
            controlPrices_ = new SampledCurve(gridPoints);
        }

        public override void calculate(IPricingEngineResults r)
        {
            var results = r as OneAssetOption.Results;
            setGridLimits();
            initializeInitialCondition();
            initializeOperator();
            initializeBoundaryConditions();
            initializeStepCondition();

            var operatorSet = new List<IOperator>();
            var arraySet = new List<Vector>();
            var bcSet = new BoundaryConditionSet();
            var conditionSet = new StepConditionSet<Vector>();

            prices_ = (SampledCurve)intrinsicValues_.Clone();

            controlPrices_ = (SampledCurve)intrinsicValues_.Clone();
            controlOperator_ = (TridiagonalOperator)finiteDifferenceOperator_.Clone();
            controlBCs_[0] = BCs_[0];
            controlBCs_[1] = BCs_[1];

            operatorSet.Add(finiteDifferenceOperator_);
            operatorSet.Add(controlOperator_);

            arraySet.Add(prices_.values());
            arraySet.Add(controlPrices_.values());

            bcSet.Add(BCs_);
            bcSet.Add(controlBCs_);

            conditionSet.Add(stepCondition_);
            conditionSet.Add(new NullCondition<Vector>());

            var model = new FiniteDifferenceModel<ParallelEvolver<CrankNicolson<TridiagonalOperator>>>(operatorSet, bcSet);

            object temp = arraySet;
            model.rollback(ref temp, getResidualTime(), 0.0, timeSteps_, conditionSet);
            arraySet = (List<Vector>)temp;

            prices_.setValues(arraySet[0]);
            controlPrices_.setValues(arraySet[1]);

            var striked_payoff = payoff_ as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(striked_payoff != null, () => "non-striked payoff given");

            var variance = process_.blackVolatility().link.blackVariance(exerciseDate_, striked_payoff.strike());
            var dividendDiscount = process_.dividendYield().link.discount(exerciseDate_);
            var riskFreeDiscount = process_.riskFreeRate().link.discount(exerciseDate_);
            var spot = process_.stateVariable().link.value();
            var forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            var black = new BlackCalculator(striked_payoff, forwardPrice, System.Math.Sqrt(variance), riskFreeDiscount);

            results.value = prices_.valueAtCenter()
                            - controlPrices_.valueAtCenter()
                            + black.value();
            results.delta = prices_.firstDerivativeAtCenter()
                            - controlPrices_.firstDerivativeAtCenter()
                            + black.delta(spot);
            results.gamma = prices_.secondDerivativeAtCenter()
                            - controlPrices_.secondDerivativeAtCenter()
                            + black.gamma(spot);
            results.additionalResults["priceCurve"] = prices_;
        }

        // required for template inheritance
        public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent) =>
            new FDStepConditionEngine(process, timeSteps, gridPoints, timeDependent);
    }
}
