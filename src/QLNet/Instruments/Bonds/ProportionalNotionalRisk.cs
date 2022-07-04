using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class ProportionalNotionalRisk : NotionalRisk
    {
        public ProportionalNotionalRisk(EventPaymentOffset paymentOffset, double attachement, double exhaustion)
            : base(paymentOffset)
        {
            attachement_ = attachement;
            exhaustion_ = exhaustion;

            Utils.QL_REQUIRE(attachement < exhaustion, () => "exhaustion level needs to be greater than attachement");
        }

        public override void updatePath(List<KeyValuePair<Date, double>> events, NotionalPath path)
        {
            path.reset();
            double losses = 0;
            double previousNotional = 1;
            for (var i = 0; i < events.Count; ++i)
            {
                losses += events[i].Value;
                if (losses > attachement_ && previousNotional > 0)
                {
                    previousNotional = System.Math.Max(0.0, (exhaustion_ - losses) / (exhaustion_ - attachement_));
                    path.addReduction(paymentOffset_.paymentDate(events[i].Key), previousNotional);
                }
            }
        }

        protected double attachement_;
        protected double exhaustion_;
    }
}