using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bkbm4M : Bkbm
    {
        public Bkbm4M(Handle<YieldTermStructure> h = null)
            : base(new Period(4, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
