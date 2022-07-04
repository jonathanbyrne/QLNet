using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class CNYCurrency : Currency
    {
        public CNYCurrency() : base("Chinese yuan", "CNY", 156, "Y", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}