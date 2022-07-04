using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public interface IFDEngine : IPricingEngine
    {
        IFDEngine factory(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100);
    }
}