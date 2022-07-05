using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class NPRCurrency : Currency
    {
        public NPRCurrency() : base("Nepal rupee", "NPR", 524, "NRs", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
