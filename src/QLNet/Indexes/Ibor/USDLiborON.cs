using QLNet.Termstructures;

namespace QLNet.Indexes.Ibor
{
    [JetBrains.Annotations.PublicAPI] public class USDLiborON : DailyTenorUSDLibor
    {
        public USDLiborON() : this(new Handle<YieldTermStructure>())
        { }

        public USDLiborON(Handle<YieldTermStructure> h) : base(0, h)
        { }

    }
}