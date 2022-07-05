using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm2M : Bkbm
    {
        public Bkbm2M(Handle<YieldTermStructure> h = null)
            : base(new Period(2, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
