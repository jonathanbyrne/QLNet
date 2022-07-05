using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class EventSet : CatRisk
    {
        private List<KeyValuePair<Date, double>> events_;
        private Date eventsEnd_;
        private Date eventsStart_;

        public EventSet(List<KeyValuePair<Date, double>> events, Date eventsStart, Date eventsEnd)
        {
            events_ = events;
            eventsStart_ = eventsStart;
            eventsEnd_ = eventsEnd;
        }

        public override CatSimulation newSimulation(Date start, Date end) => new EventSetSimulation(events_, eventsStart_, eventsEnd_, start, end);
    }
}
