using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class PKRCurrency : Currency
    {
        public PKRCurrency() : base("Pakistani rupee", "PKR", 586, "Rs", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
