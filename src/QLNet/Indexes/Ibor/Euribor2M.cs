using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class Euribor2M : Euribor
    {
        public Euribor2M() : this(new Handle<YieldTermStructure>()) { }
        public Euribor2M(Handle<YieldTermStructure> h) : base(new Period(2, TimeUnit.Months), h) { }
    }
}