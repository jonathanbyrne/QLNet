using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class MYRCurrency : Currency
    {
        public MYRCurrency() : base("Malaysian Ringgit", "MYR", 458, "RM", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}