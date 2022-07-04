using QLNet.Math;

namespace QLNet.Currencies
{
    /// Spanish peseta
    /// The ISO three-letter code was ESP; the numeric code was 724.
    /// It was divided in 100 centimos.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class ESPCurrency : Currency
    {
        public ESPCurrency() : base("Spanish peseta", "ESP", 724, "Pta", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency()) { }
    }
}