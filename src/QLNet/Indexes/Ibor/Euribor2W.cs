using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Euribor2W : Euribor
    {
        public Euribor2W() : this(new Handle<YieldTermStructure>())
        {
        }

        public Euribor2W(Handle<YieldTermStructure> h) : base(new Period(2, TimeUnit.Weeks), h)
        {
        }
    }
}
