using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm3M : Bkbm
    {
        public Bkbm3M(Handle<YieldTermStructure> h = null)
            : base(new Period(3, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
