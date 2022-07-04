using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// Icelandic krona
    /// The ISO three-letter code is ISK; the numeric code is 352.
    /// It is divided in 100 aurar.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class ISKCurrency : Currency
    {
        public ISKCurrency() : base("Iceland krona", "ISK", 352, "IKr", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}