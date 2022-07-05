using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet
{
    [PublicAPI]
    public interface IGenericEngine : IPricingEngine, IObserver
    {
    }
}
