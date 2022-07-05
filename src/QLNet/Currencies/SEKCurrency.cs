using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Swedish krona
    /// The ISO three-letter code is SEK; the numeric code is 752.
    /// It is divided in 100 цre.
    [PublicAPI]
    public class SEKCurrency : Currency
    {
        public SEKCurrency() : base("Swedish krona", "SEK", 752, "kr", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
