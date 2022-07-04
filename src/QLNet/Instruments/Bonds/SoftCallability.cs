using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class SoftCallability : Callability
    {
        public SoftCallability(Price price, Date date, double trigger)
            : base(price, Type.Call, date)
        {
            trigger_ = trigger;
        }

        public double trigger() => trigger_;

        private double trigger_;
    }
}