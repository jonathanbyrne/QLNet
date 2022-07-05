using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Cyprus pound
    ///     The ISO three-letter code is CYP; the numeric code is 196.
    ///     It is divided in 100 cents.
    /// </summary>
    [PublicAPI]
    public class CYPCurrency : Currency
    {
        public CYPCurrency() : base("Cyprus pound", "CYP", 196, "\xA3C", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
