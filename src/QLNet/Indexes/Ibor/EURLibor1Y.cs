using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EURLibor1Y : EURLibor
    {
        public EURLibor1Y()
            : base(new Period(1, TimeUnit.Years), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor1Y(Handle<YieldTermStructure> h)
            : base(new Period(1, TimeUnit.Years), h)
        {
        }
    }
}
