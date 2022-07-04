using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class Euribor1Y : Euribor
    {
        public Euribor1Y() : this(new Handle<YieldTermStructure>()) { }
        public Euribor1Y(Handle<YieldTermStructure> h) : base(new Period(1, TimeUnit.Years), h) { }

    }
}