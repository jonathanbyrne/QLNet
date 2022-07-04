using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// Maltese lira
    /// The ISO three-letter code is MTL; the numeric code is 470.
    /// It is divided in 100 cents.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class MTLCurrency : Currency
    {
        public MTLCurrency() : base("Maltese lira", "MTL", 470, "Lm", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}