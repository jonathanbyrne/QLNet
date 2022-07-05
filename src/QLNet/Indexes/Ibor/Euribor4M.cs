using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Euribor4M : Euribor
    {
        public Euribor4M() : this(new Handle<YieldTermStructure>())
        {
        }

        public Euribor4M(Handle<YieldTermStructure> h) : base(new Period(4, TimeUnit.Months), h)
        {
        }
    }
}
