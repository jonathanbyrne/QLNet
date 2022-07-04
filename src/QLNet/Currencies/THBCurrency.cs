using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class THBCurrency : Currency
    {
        public THBCurrency() : base("Thai baht", "THB", 764, "Bht", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}