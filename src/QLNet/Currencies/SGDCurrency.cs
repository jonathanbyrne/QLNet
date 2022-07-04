using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class SGDCurrency : Currency
    {
        public SGDCurrency() : base("Singapore dollar", "SGD", 702, "S$", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}