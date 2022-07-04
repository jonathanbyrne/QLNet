using QLNet.Math;

namespace QLNet.Currencies
{
    /// Belgian franc
    /// The ISO three-letter code was BEF; the numeric code was 56.
    /// It had no subdivisions.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class BEFCurrency : Currency
    {
        public BEFCurrency() : base("Belgian franc", "BEF", 56, "", "", 1, new Rounding(), "%2% %1$.0f", new EURCurrency()) { }
    }
}