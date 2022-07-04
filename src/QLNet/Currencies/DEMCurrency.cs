using QLNet.Math;

namespace QLNet.Currencies
{
    /// Deutsche mark
    /// The ISO three-letter code was DEM; the numeric code was 276.
    /// It was divided into 100 pfennig.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class DEMCurrency : Currency
    {
        public DEMCurrency() : base("Deutsche mark", "DEM", 276, "DM", "", 100, new Rounding(), "%1$.2f %3%", new EURCurrency()) { }
    }
}