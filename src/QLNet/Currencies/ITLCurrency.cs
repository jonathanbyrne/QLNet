using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Italian lira
    /// The ISO three-letter code was ITL; the numeric code was 380.
    /// It had no subdivisions.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class ITLCurrency : Currency
    {
        public ITLCurrency() : base("Italian lira", "ITL", 380, "L", "", 1, new Rounding(), "%3% %1$.0f", new EURCurrency())
        {
        }
    }
}
