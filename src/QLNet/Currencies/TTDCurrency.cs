using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class TTDCurrency : Currency
    {
        public TTDCurrency() : base("Trinidad & Tobago dollar", "TTD", 780, "TT$", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
