using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class simple_event : Event
    {
        private Date date_;

        public simple_event(Date date)
        {
            date_ = date;
        }

        public override Date date() => date_;
    }
}
