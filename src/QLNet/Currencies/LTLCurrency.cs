using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Lithuanian litas
    ///     The ISO three-letter code is LTL; the numeric code is 440.
    ///     It is divided in 100 centu.
    /// </summary>
    [PublicAPI]
    public class LTLCurrency : Currency
    {
        public LTLCurrency() : base("Lithuanian litas", "LTL", 440, "Lt", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
