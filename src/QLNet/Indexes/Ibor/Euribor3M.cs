using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class Euribor3M : Euribor
    {
        public Euribor3M() : this(new Handle<YieldTermStructure>())
        {
        }

        public Euribor3M(Handle<YieldTermStructure> h) : base(new Period(3, TimeUnit.Months), h)
        {
        }
    }
}
