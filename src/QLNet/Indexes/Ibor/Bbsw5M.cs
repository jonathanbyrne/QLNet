using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [JetBrains.Annotations.PublicAPI] public class Bbsw5M : Bbsw
    {
        public Bbsw5M(Handle<YieldTermStructure> h = null)
            : base(new Period(5, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        { }
    }
}