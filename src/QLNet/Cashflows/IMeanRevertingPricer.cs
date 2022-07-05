using JetBrains.Annotations;
using QLNet.Quotes;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public interface IMeanRevertingPricer
    {
        double meanReversion();

        void setMeanReversion(Handle<Quote> q);
    }
}
