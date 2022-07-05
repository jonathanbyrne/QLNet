using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm6M : Bkbm
    {
        public Bkbm6M(Handle<YieldTermStructure> h = null)
            : base(new Period(6, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
