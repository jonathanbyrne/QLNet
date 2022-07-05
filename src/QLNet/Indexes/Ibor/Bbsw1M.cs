using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bbsw1M : Bbsw
    {
        public Bbsw1M(Handle<YieldTermStructure> h = null)
            : base(new Period(1, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
