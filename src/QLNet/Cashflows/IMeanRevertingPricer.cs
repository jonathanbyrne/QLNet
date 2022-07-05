using JetBrains.Annotations;
using QLNet.Quotes;

namespace QLNet
{
    [PublicAPI]
    public interface IMeanRevertingPricer
    {
        double meanReversion();

        void setMeanReversion(Handle<Quote> q);
    }
}
