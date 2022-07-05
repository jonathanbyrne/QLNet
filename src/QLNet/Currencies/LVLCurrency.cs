using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Latvian lat
    ///     The ISO three-letter code is LVL; the numeric code is 428.
    ///     It is divided in 100 santims.
    /// </summary>
    [PublicAPI]
    public class LVLCurrency : Currency
    {
        public LVLCurrency() : base("Latvian lat", "LVL", 428, "Ls", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
