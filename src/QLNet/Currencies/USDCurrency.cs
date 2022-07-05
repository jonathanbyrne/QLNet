using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class USDCurrency : Currency
    {
        public USDCurrency() : base("U.S. dollar", "USD", 840, "$", "\xA2", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
