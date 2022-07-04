using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    public abstract class NotionalRisk
    {
        protected NotionalRisk(EventPaymentOffset paymentOffset)
        {
            paymentOffset_ = paymentOffset;
        }

        public abstract void updatePath(List<KeyValuePair<Date, double>> events, NotionalPath path);

        protected EventPaymentOffset paymentOffset_;
    }
}