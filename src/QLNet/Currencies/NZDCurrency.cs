using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class NZDCurrency : Currency
    {
        public NZDCurrency() : base("New Zealand dollar", "NZD", 554, "NZ$", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}