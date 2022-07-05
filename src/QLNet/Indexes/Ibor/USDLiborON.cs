using JetBrains.Annotations;
using QLNet.Termstructures;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class USDLiborON : DailyTenorUSDLibor
    {
        public USDLiborON() : this(new Handle<YieldTermStructure>())
        {
        }

        public USDLiborON(Handle<YieldTermStructure> h) : base(0, h)
        {
        }
    }
}
