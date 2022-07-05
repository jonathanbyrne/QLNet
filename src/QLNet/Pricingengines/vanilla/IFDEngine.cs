using JetBrains.Annotations;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public interface IFDEngine : IPricingEngine
    {
        IFDEngine factory(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100);
    }
}
