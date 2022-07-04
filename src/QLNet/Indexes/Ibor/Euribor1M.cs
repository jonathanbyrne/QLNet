using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class Euribor1M : Euribor
    {
        public Euribor1M() : this(new Handle<YieldTermStructure>()) { }
        public Euribor1M(Handle<YieldTermStructure> h) : base(new Period(1, TimeUnit.Months), h) { }
    }
}