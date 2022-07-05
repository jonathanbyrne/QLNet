using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet
{
    [PublicAPI]
    public interface IPricingEngine : IObservable
    {
        void calculate();

        IPricingEngineArguments getArguments();

        IPricingEngineResults getResults();

        void reset();
    }
}
