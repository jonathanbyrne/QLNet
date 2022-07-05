using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class IDRCurrency : Currency
    {
        public IDRCurrency() : base("Indonesian Rupiah", "IDR", 360, "Rp", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
