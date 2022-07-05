using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class TWDCurrency : Currency
    {
        public TWDCurrency() : base("Taiwan dollar", "TWD", 901, "NT$", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
