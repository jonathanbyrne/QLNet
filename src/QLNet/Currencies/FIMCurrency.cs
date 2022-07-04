using QLNet.Math;

namespace QLNet.Currencies
{
    /// Finnish markka
    /// The ISO three-letter code was FIM; the numeric code was 246.
    /// It was divided in 100 penniд.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class FIMCurrency : Currency
    {
        public FIMCurrency() : base("Finnish markka", "FIM", 246, "mk", "", 100, new Rounding(), "%1$.2f %3%", new EURCurrency()) { }
    }
}