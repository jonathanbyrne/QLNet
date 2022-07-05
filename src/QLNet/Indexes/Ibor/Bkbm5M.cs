using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm5M : Bkbm
    {
        public Bkbm5M(Handle<YieldTermStructure> h = null)
            : base(new Period(5, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
