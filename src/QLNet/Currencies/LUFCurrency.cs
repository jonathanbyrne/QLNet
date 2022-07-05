using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Luxembourg franc
    /// The ISO three-letter code was LUF; the numeric code was 442.
    /// It was divided in 100 centimes.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class LUFCurrency : Currency
    {
        public LUFCurrency() : base("Luxembourg franc", "LUF", 442, "F", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency())
        {
        }
    }
}
