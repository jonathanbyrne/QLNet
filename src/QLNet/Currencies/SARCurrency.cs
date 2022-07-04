using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class SARCurrency : Currency
    {
        public SARCurrency() : base("Saudi riyal", "SAR", 682, "SRls", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}