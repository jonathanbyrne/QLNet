using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class KWDCurrency : Currency
    {
        public KWDCurrency() : base("Kuwaiti dinar", "KWD", 414, "KD", "", 1000, new Rounding(), "%3% %1$.3f") { }
    }
}