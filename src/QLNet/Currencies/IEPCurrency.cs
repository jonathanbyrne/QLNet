using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Irish punt
    /// The ISO three-letter code was IEP; the numeric code was 372.
    /// It was divided in 100 pence.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class IEPCurrency : Currency
    {
        public IEPCurrency() : base("Irish punt", "IEP", 372, "", "", 100, new Rounding(), "%2% %1$.2f", new EURCurrency())
        {
        }
    }
}
