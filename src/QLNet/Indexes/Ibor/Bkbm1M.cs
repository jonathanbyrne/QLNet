using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm1M : Bkbm
    {
        public Bkbm1M(Handle<YieldTermStructure> h = null)
            : base(new Period(1, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
