using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class IRRCurrency : Currency
    {
        public IRRCurrency() : base("Iranian rial", "IRR", 364, "Rls", "", 1, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
