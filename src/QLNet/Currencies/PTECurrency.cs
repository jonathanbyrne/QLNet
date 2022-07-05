using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Portuguese escudo
    /// The ISO three-letter code was PTE; the numeric code was 620.
    /// It was divided in 100 centavos.
    /// Obsoleted by the Euro since 1999.
    [PublicAPI]
    public class PTECurrency : Currency
    {
        public PTECurrency() : base("Portuguese escudo", "PTE", 620, "Esc", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency())
        {
        }
    }
}
