﻿/*
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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    //! Finite-differences pricing engine for shout vanilla options
    /*! \ingroup vanillaengines

        \test the correctness of the returned greeks is tested by
              reproducing numerical derivatives.
    */
    [PublicAPI]
    public class FDShoutEngine : FDEngineAdapter<FDShoutCondition<FDStepConditionEngine>, OneAssetOption.Engine>,
        IFDEngine
    {
        // required for generics
        public FDShoutEngine()
        {
        }

        public FDShoutEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints,
            bool timeDependent = false)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
        }

        public IFDEngine factory(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100) => new FDShoutEngine(process, timeSteps, gridPoints);
    }
}
