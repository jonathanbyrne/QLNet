using QLNet.Quotes;

namespace QLNet.Termstructures
{
    [JetBrains.Annotations.PublicAPI] public class RateHelper : BootstrapHelper<YieldTermStructure>
    {
        public RateHelper() : base() { } // required for generics
        public RateHelper(Handle<Quote> quote) : base(quote) { }
        public RateHelper(double quote) : base(quote) { }
    }
}