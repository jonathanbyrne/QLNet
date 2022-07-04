using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class INRCurrency : Currency
    {
        public INRCurrency()
            : base("Indian rupee", "INR", 356, "Rs", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}