using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Belarussian ruble
    ///     The ISO three-letter code is BYR; the numeric code is 974.
    ///     It has no subdivisions.
    /// </summary>
    [PublicAPI]
    public class BYRCurrency : Currency
    {
        public BYRCurrency() : base("Belarussian ruble", "BYR", 974, "BR", "", 1, new Rounding(), "%2% %1$.0f")
        {
        }
    }
}
