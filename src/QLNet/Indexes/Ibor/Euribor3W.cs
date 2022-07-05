using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Euribor3W : Euribor
    {
        public Euribor3W() : this(new Handle<YieldTermStructure>())
        {
        }

        public Euribor3W(Handle<YieldTermStructure> h) : base(new Period(3, TimeUnit.Weeks), h)
        {
        }
    }
}
