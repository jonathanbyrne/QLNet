using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// Czech koruna
    /// The ISO three-letter code is CZK; the numeric code is 203.
    /// It is divided in 100 haleru.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class CZKCurrency : Currency
    {
        public CZKCurrency() : base("Czech koruna", "CZK", 203, "Kc", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}