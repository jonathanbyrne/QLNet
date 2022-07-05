using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class KRWCurrency : Currency
    {
        public KRWCurrency() : base("South-Korean won", "KRW", 410, "W", "", 100, new Rounding(), "%3% %1$.0f")
        {
        }
    }
}
