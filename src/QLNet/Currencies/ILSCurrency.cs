using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class ILSCurrency : Currency
    {
        public ILSCurrency() : base("Israeli shekel", "ILS", 376, "NIS", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}