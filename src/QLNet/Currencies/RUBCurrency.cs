using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class RUBCurrency : Currency
    {
        public RUBCurrency() : base("Russian ruble", "RUB", 643, "", "", 100, new Rounding(), "%1$.2f %2%")
        {
        }
    }
}
