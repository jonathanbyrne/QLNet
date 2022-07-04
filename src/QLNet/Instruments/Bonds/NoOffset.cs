using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class NoOffset : EventPaymentOffset
    {
        public override Date paymentDate(Date eventDate) => eventDate;
    }
}