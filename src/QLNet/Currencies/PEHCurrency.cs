using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class PEHCurrency : Currency
    {
        public PEHCurrency() : base("Peruvian sol", "PEH", 999, "S./", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
