using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class simple_event : Event
    {
        public simple_event(Date date)
        {
            date_ = date;
        }
        public override Date date() => date_;

        private Date date_;

    }
}