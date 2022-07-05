using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class SARCurrency : Currency
    {
        public SARCurrency() : base("Saudi riyal", "SAR", 682, "SRls", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
