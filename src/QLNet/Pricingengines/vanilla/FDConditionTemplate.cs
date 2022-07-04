﻿using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class FDConditionTemplate<baseEngine> : FDConditionEngineTemplate
        where baseEngine : FDConditionEngineTemplate, new()
    {
        #region Common definitions for deriving classes
        protected baseEngine engine_;

        // below is a wrap-up of baseEngine instead of c++ template inheritance
        public override void setupArguments(IPricingEngineArguments a) { engine_.setupArguments(a); }
        public override void calculate(IPricingEngineResults r) { engine_.calculate(r); }
        #endregion

        // required for generics
        public FDConditionTemplate() { }

        public FDConditionTemplate(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            // init engine
            engine_ = (baseEngine)FastActivator<baseEngine>.Create().factory(process, timeSteps, gridPoints, timeDependent);
        }
    }
}