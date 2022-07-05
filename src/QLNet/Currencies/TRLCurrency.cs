using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Turkish lira
    /// The ISO three-letter code was TRL; the numeric code was 792.
    /// It was divided in 100 kurus.
    /// Obsoleted by the new Turkish lira since 2005.
    [PublicAPI]
    public class TRLCurrency : Currency
    {
        public TRLCurrency() : base("Turkish lira", "TRL", 792, "TL", "", 100, new Rounding(), "%1$.0f %3%")
        {
        }
    }
}
