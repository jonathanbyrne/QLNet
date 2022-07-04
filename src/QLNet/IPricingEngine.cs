using QLNet.Patterns;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public interface IPricingEngine : IObservable
    {
        IPricingEngineArguments getArguments();
        IPricingEngineResults getResults();
        void reset();
        void calculate();
    }
}