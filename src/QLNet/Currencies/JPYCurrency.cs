using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Japanese yen
    ///     The ISO three-letter code is JPY; the numeric code is 392.
    ///     It is divided into 100 sen.
    /// </summary>
    [PublicAPI]
    public class JPYCurrency : Currency
    {
        public JPYCurrency() : base("Japanese yen", "JPY", 392, "\xA5", "", 100, new Rounding(), "%3% %1$.0f")
        {
        }
    }
}
