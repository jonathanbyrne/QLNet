using JetBrains.Annotations;
using QLNet.Termstructures;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class GBPLiborON : DailyTenorGBPLibor
    {
        public GBPLiborON(Handle<YieldTermStructure> h) : base(0, h)
        {
        }
    }
}
