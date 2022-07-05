using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EuriborSW : Euribor
    {
        public EuriborSW() : this(new Handle<YieldTermStructure>())
        {
        }

        public EuriborSW(Handle<YieldTermStructure> h) : base(new Period(1, TimeUnit.Weeks), h)
        {
        }
    }
}
