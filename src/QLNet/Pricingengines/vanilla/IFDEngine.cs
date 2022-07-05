using JetBrains.Annotations;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public interface IFDEngine : IPricingEngine
    {
        IFDEngine factory(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100);
    }
}
