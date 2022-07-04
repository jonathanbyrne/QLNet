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
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.processes;
using System;

namespace QLNet.Pricingengines.vanilla
{
    // this is template version to serve as base for FDStepConditionEngine and FDMultiPeriodEngine
    [JetBrains.Annotations.PublicAPI] public class FDConditionEngineTemplate : FDVanillaEngine
    {
        #region Common definitions for deriving classes
        protected IStepCondition<Vector> stepCondition_;
        protected SampledCurve prices_;
        protected virtual void initializeStepCondition()
        {
            if (stepConditionImpl_ == null)
                stepCondition_ = new NullCondition<Vector>();
            else
                stepCondition_ = stepConditionImpl_();
        }

        protected Func<IStepCondition<Vector>> stepConditionImpl_;
        public void setStepCondition(Func<IStepCondition<Vector>> impl)
        {
            stepConditionImpl_ = impl;
        }
        #endregion

        // required for generics
        public FDConditionEngineTemplate() { }

        public FDConditionEngineTemplate(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
           : base(process, timeSteps, gridPoints, timeDependent) { }
    }

    // this is template version to serve as base for FDAmericanCondition and FDShoutCondition
}
