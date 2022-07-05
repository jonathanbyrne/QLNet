using JetBrains.Annotations;

namespace QLNet
{
    [PublicAPI]
    public interface IPricingEngineArguments
    {
        void validate();
    }
}
