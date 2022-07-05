using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class FDShoutCondition<baseEngine> : FDConditionTemplate<baseEngine>
        where baseEngine : FDConditionEngineTemplate, new()
    {
        // required for generics
        public FDShoutCondition()
        {
        }

        public FDShoutCondition(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            engine_.setStepCondition(initializeStepConditionImpl);
        }

        // required for template inheritance
        public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent) =>
            new FDShoutCondition<baseEngine>(process, timeSteps, gridPoints, timeDependent);

        protected IStepCondition<Vector> initializeStepConditionImpl()
        {
            // the following to rely on process_ which is the same for engine and here
            // therefore wrapping is not requried
            var residualTime = engine_.getResidualTime();
            var riskFreeRate = process_.riskFreeRate().link.zeroRate(residualTime, Compounding.Continuous).rate();

            return new ShoutCondition(engine_.intrinsicValues_.values(), residualTime, riskFreeRate);
        }
    }
}
