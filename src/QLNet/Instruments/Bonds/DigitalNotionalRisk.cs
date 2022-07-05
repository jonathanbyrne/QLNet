using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class DigitalNotionalRisk : NotionalRisk
    {
        protected double threshold_;

        public DigitalNotionalRisk(EventPaymentOffset paymentOffset, double threshold)
            : base(paymentOffset)
        {
            threshold_ = threshold;
        }

        public override void updatePath(List<KeyValuePair<Date, double>> events,
            NotionalPath path)
        {
            path.reset();
            for (var i = 0; i < events.Count; ++i)
            {
                if (events[i].Value >= threshold_)
                {
                    path.addReduction(paymentOffset_.paymentDate(events[i].Key), 0.0);
                }
            }
        }
    }
}
