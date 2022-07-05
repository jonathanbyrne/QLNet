using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EURLibor2W : EURLibor
    {
        public EURLibor2W()
            : base(new Period(2, TimeUnit.Weeks), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor2W(Handle<YieldTermStructure> h)
            : base(new Period(2, TimeUnit.Weeks), h)
        {
        }
    }
}
