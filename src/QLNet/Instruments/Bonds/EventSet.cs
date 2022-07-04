using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class EventSet : CatRisk
    {
        public EventSet(List<KeyValuePair<Date, double>> events, Date eventsStart, Date eventsEnd)
        {
            events_ = events;
            eventsStart_ = eventsStart;
            eventsEnd_ = eventsEnd;
        }

        public override CatSimulation newSimulation(Date start, Date end) => new EventSetSimulation(events_, eventsStart_, eventsEnd_, start, end);

        private List<KeyValuePair<Date, double>> events_;
        private Date eventsStart_;
        private Date eventsEnd_;
    }
}