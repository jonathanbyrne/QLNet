using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class Euribor5M : Euribor
    {
        public Euribor5M() : this(new Handle<YieldTermStructure>()) { }
        public Euribor5M(Handle<YieldTermStructure> h) : base(new Period(5, TimeUnit.Months), h) { }
    }
}