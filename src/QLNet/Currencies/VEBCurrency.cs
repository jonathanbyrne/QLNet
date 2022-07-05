using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class VEBCurrency : Currency
    {
        public VEBCurrency() : base("Venezuelan bolivar", "VEB", 862, "Bs", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
