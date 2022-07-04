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
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.processes;
using System;
using QLNet.Methods.Finitedifferences;

namespace QLNet.Pricingengines.vanilla
{
    //! Finite-differences Bermudan engine
    /*! \ingroup vanillaengines */
    [JetBrains.Annotations.PublicAPI] public class FDBermudanEngine : FDMultiPeriodEngine, IGenericEngine
    {
        protected double extraTermInBermudan;

        // constructor
        public FDBermudanEngine(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100,
                                bool timeDependent = false)
           : base(process, timeSteps, gridPoints, timeDependent) { }

        public void calculate()
        {
            setupArguments(arguments_);
            base.calculate(results_);
        }

        protected override void initializeStepCondition()
        {
            stepCondition_ = new NullCondition<Vector>();
        }

        protected override void executeIntermediateStep(int i)
        {
            var size = intrinsicValues_.size();
            for (var j = 0; j < size; j++)
                prices_.setValue(j, System.Math.Max(prices_.value(j), intrinsicValues_.value(j)));
        }

        #region IGenericEngine copy-cat
        protected QLNet.Option.Arguments arguments_ = new QLNet.Option.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

        public IPricingEngineArguments getArguments() => arguments_;

        public IPricingEngineResults getResults() => results_;

        public void reset() { results_.reset(); }

        #region Observer & Observable
        // observable interface
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public void update() { notifyObservers(); }
        #endregion
        #endregion
    }
}
