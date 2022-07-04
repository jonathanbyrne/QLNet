using QLNet.Math;

namespace QLNet.Currencies
{
    /// French franc
    /// The ISO three-letter code was FRF; the numeric code was 250.
    /// It was divided in 100 centimes.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class FRFCurrency : Currency
    {
        public FRFCurrency() : base("French franc", "FRF", 250, "", "", 100, new Rounding(), "%1$.2f %2%", new EURCurrency()) { }
    }
}