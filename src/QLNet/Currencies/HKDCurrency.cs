using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class HKDCurrency : Currency
    {
        public HKDCurrency() : base("Hong Kong dollar", "HKD", 344, "HK$", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
