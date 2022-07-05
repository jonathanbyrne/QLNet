using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Danish krone
    ///     The ISO three-letter code is DKK; the numeric code is 208.
    ///     It is divided in 100 шre.
    /// </summary>
    [PublicAPI]
    public class DKKCurrency : Currency
    {
        public DKKCurrency() : base("Danish krone", "DKK", 208, "Dkr", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
