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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Patterns;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    //! Pricing engine for European options using finite-differences
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              checking it against analytic results.
    */
    [PublicAPI]
    public class FDEuropeanEngine : FDVanillaEngine, IGenericEngine
    {
        private SampledCurve prices_;

        public FDEuropeanEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints)
            : this(process, timeSteps, gridPoints, false)
        {
        }

        public FDEuropeanEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            prices_ = new SampledCurve(gridPoints);

            process.registerWith(update);
        }

        public void calculate()
        {
            setupArguments(arguments_);
            setGridLimits();
            initializeInitialCondition();
            initializeOperator();
            initializeBoundaryConditions();

            var model = new FiniteDifferenceModel<CrankNicolson<TridiagonalOperator>>(finiteDifferenceOperator_, BCs_);

            prices_ = (SampledCurve)intrinsicValues_.Clone();

            // this is a workaround for pointers to avoid unsafe code
            // in the grid calculation Vector temp goes through many operations
            object temp = prices_.values();
            model.rollback(ref temp, getResidualTime(), 0, timeSteps_);
            prices_.setValues((Vector)temp);

            results_.value = prices_.valueAtCenter();
            results_.delta = prices_.firstDerivativeAtCenter();
            results_.gamma = prices_.secondDerivativeAtCenter();
            results_.theta = Utils.blackScholesTheta(process_,
                results_.value.GetValueOrDefault(),
                results_.delta.GetValueOrDefault(),
                results_.gamma.GetValueOrDefault());
            results_.additionalResults["priceCurve"] = prices_;
        }

        #region IGenericEngine copy-cat

        protected QLNet.Option.Arguments arguments_ = new QLNet.Option.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

        public IPricingEngineArguments getArguments() => arguments_;

        public IPricingEngineResults getResults() => results_;

        public void reset()
        {
            results_.reset();
        }

        #region Observer & Observable

        // observable interface
        private readonly WeakEventSource eventSource = new WeakEventSource();

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public void update()
        {
            notifyObservers();
        }

        #endregion

        #endregion
    }
}
