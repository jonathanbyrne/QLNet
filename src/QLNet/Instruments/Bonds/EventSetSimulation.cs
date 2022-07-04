using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class EventSetSimulation : CatSimulation
    {
        public EventSetSimulation(List<KeyValuePair<Date, double>> events, Date eventsStart, Date eventsEnd, Date start, Date end)
            : base(start, end)
        {
            events_ = events;
            eventsStart_ = eventsStart;
            eventsEnd_ = eventsEnd;
            i_ = 0;

            years_ = end_.year() - start_.year();
            if (eventsStart_.month() < start_.month() ||
                eventsStart_.month() == start_.month() && eventsStart_.Day <= start_.Day)
            {
                periodStart_ = new Date(start_.Day, start_.Month, eventsStart_.Year);
            }
            else
            {
                periodStart_ = new Date(start_.Day, start_.month(), eventsStart_.year() + 1);
            }
            periodEnd_ = new Date(end_.Day, end_.Month, periodStart_.Year + years_);
            while (i_ < events_.Count && events_[i_].Key < periodStart_)
                ++i_; //i points to the first element after the start of the relevant period.

        }

        public override bool nextPath(List<KeyValuePair<Date, double>> path)
        {
            path.Clear();
            if (periodEnd_ > eventsEnd_) //Ran out of event data
                return false;

            while (i_ < events_.Count && events_[i_].Key < periodStart_)
            {
                ++i_; //skip the elements between the previous period and this period
            }
            while (i_ < events_.Count && events_[i_].Key <= periodEnd_)
            {
                var e = new KeyValuePair<Date, double>
                    (events_[i_].Key + new Period(start_.year() - periodStart_.year(), TimeUnit.Years), events_[i_].Value);
                path.Add(e);
                ++i_; //i points to the first element after the start of the relevant period.
            }
            if (start_ + new Period(years_, TimeUnit.Years) < end_)
            {
                periodStart_ += new Period(years_ + 1, TimeUnit.Years);
                periodEnd_ += new Period(years_ + 1, TimeUnit.Years);
            }
            else
            {
                periodStart_ += new Period(years_, TimeUnit.Years);
                periodEnd_ += new Period(years_, TimeUnit.Years);
            }
            return true;
        }

        private List<KeyValuePair<Date, double>> events_;
        private Date eventsStart_;
        private Date eventsEnd_;

        private int years_;
        private Date periodStart_;
        private Date periodEnd_;
        private int i_;
    }
}