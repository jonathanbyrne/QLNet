using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Polish zloty
    /// The ISO three-letter code is PLN; the numeric code is 985.
    /// It is divided in 100 groszy.
    [PublicAPI]
    public class PLNCurrency : Currency
    {
        public PLNCurrency() : base("Polish zloty", "PLN", 985, "zl", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
