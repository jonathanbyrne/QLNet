using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Romanian leu
    /// The ISO three-letter code was ROL; the numeric code was 642.
    /// It was divided in 100 bani.
    [PublicAPI]
    public class ROLCurrency : Currency
    {
        public ROLCurrency() : base("Romanian leu", "ROL", 642, "L", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
