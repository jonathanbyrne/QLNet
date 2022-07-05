using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Slovenian tolar
    /// The ISO three-letter code is SIT; the numeric code is 705.
    /// It is divided in 100 stotinov.
    [PublicAPI]
    public class SITCurrency : Currency
    {
        public SITCurrency() : base("Slovenian tolar", "SIT", 705, "SlT", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
