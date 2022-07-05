using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     European Euro
    ///     The ISO three-letter code is EUR; the numeric code is 978.
    ///     It is divided into 100 cents.
    /// </summary>
    [PublicAPI]
    public class EURCurrency : Currency
    {
        public EURCurrency() : base("European Euro", "EUR", 978, "", "", 100, new Rounding(2, Rounding.Type.Closest), "%2% %1$.2f")
        {
        }
    }
}
