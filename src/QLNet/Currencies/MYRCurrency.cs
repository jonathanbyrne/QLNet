using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class MYRCurrency : Currency
    {
        public MYRCurrency() : base("Malaysian Ringgit", "MYR", 458, "RM", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
