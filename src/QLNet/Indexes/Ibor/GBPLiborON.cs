using QLNet.Termstructures;

namespace QLNet.Indexes.Ibor
{
    [JetBrains.Annotations.PublicAPI] public class GBPLiborON : DailyTenorGBPLibor
    {
        public GBPLiborON(Handle<YieldTermStructure> h) : base(0, h)
        { }
    }
}