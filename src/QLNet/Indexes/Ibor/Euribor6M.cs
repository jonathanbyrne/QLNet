using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Euribor6M : Euribor
    {
        public Euribor6M() : this(new Handle<YieldTermStructure>())
        {
        }

        public Euribor6M(Handle<YieldTermStructure> h) : base(new Period(6, TimeUnit.Months), h)
        {
        }
    }
}
