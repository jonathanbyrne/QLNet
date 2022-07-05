using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Dutch guilder
    /// The ISO three-letter code was NLG; the numeric code was 528.
    /// It was divided in 100 cents.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class NLGCurrency : Currency
    {
        public NLGCurrency() : base("Dutch guilder", "NLG", 528, "f", "", 100, new Rounding(), "%3% %1$.2f", new EURCurrency())
        {
        }
    }
}
