using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class SoftCallability : Callability
    {
        private double trigger_;

        public SoftCallability(Price price, Date date, double trigger)
            : base(price, Type.Call, date)
        {
            trigger_ = trigger;
        }

        public double trigger() => trigger_;
    }
}
