using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Swiss franc
    ///     The ISO three-letter code is CHF; the numeric code is 756.
    ///     It is divided into 100 cents.
    /// </summary>
    [PublicAPI]
    public class CHFCurrency : Currency
    {
        public CHFCurrency() : base("Swiss franc", "CHF", 756, "SwF", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
