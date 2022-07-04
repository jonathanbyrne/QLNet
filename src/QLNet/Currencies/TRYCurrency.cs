using QLNet.Math;

namespace QLNet.Currencies
{
    /// New Turkish lira
    /// The ISO three-letter code is TRY; the numeric code is 949.
    ///  It is divided in 100 new kurus.
    [JetBrains.Annotations.PublicAPI] public class TRYCurrency : Currency
    {
        public TRYCurrency() : base("New Turkish lira", "TRY", 949, "YTL", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}