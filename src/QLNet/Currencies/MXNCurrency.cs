using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class MXNCurrency : Currency
    {
        public MXNCurrency() : base("Mexican peso", "MXN", 484, "Mex$", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
