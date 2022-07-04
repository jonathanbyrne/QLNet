using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class VNDCurrency : Currency
    {
        public VNDCurrency() : base("Vietnamese Dong", "VND", 704, "", "", 100, new Rounding(), "%1$.0f %3%") { }
    }
}