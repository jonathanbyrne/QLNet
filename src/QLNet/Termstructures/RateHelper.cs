using JetBrains.Annotations;
using QLNet.Quotes;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public class RateHelper : BootstrapHelper<YieldTermStructure>
    {
        public RateHelper()
        {
        } // required for generics

        public RateHelper(Handle<Quote> quote) : base(quote)
        {
        }

        public RateHelper(double quote) : base(quote)
        {
        }
    }
}
