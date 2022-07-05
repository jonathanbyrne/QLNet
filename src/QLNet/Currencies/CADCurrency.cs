using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class CADCurrency : Currency
    {
        public CADCurrency() : base("Canadian dollar", "CAD", 124, "Can$", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
