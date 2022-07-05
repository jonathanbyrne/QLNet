using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class FDAmericanCondition<baseEngine> : FDConditionTemplate<baseEngine>
        where baseEngine : FDConditionEngineTemplate, new()
    {
        // required for generics
        public FDAmericanCondition()
        {
        }

        public FDAmericanCondition(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            engine_.setStepCondition(initializeStepConditionImpl);
        }

        // required for template inheritance
        public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent) =>
            new FDAmericanCondition<baseEngine>(process, timeSteps, gridPoints, timeDependent);

        protected IStepCondition<Vector> initializeStepConditionImpl() => new AmericanCondition(engine_.intrinsicValues_.values());
    }
}
