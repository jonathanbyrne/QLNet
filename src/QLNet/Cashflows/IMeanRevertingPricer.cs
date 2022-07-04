using QLNet.Quotes;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public interface IMeanRevertingPricer
    {
        double meanReversion() ;
        void setMeanReversion(Handle<Quote> q) ;
    }
}