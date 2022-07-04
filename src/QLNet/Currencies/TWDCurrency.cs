using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class TWDCurrency : Currency
    {
        public TWDCurrency() : base("Taiwan dollar", "TWD", 901, "NT$", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}