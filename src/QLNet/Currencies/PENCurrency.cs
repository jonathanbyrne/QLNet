using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class PENCurrency : Currency
    {
        public PENCurrency() : base("Peruvian nuevo sol", "PEN", 604, "S/.", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
