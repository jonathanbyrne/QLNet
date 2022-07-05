using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Greek drachma
    /// The ISO three-letter code was GRD; the numeric code was 300.
    /// It was divided in 100 lepta.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class GRDCurrency : Currency
    {
        public GRDCurrency() : base("Greek drachma", "GRD", 300, "", "", 100, new Rounding(), "%1$.2f %2%", new EURCurrency())
        {
        }
    }
}
