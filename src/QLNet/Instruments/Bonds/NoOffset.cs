using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class NoOffset : EventPaymentOffset
    {
        public override Date paymentDate(Date eventDate) => eventDate;
    }
}
