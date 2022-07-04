using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// Hungarian forint
    /// The ISO three-letter code is HUF; the numeric code is 348.
    /// It has no subdivisions.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class HUFCurrency : Currency
    {
        public HUFCurrency() : base("Hungarian forint", "HUF", 348, "Ft", "", 1, new Rounding(), "%1$.0f %3%") { }
    }
}