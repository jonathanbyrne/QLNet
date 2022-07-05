using QLNet.Time;

namespace QLNet.Indexes
{
    public partial class Utils
    {
        public static Date nextWednesday(Date date) => previousWednesday(date + 7);

        public static Date previousWednesday(Date date)
        {
            var w = date.weekday();
            if (w >= 4) // roll back w-4 days
            {
                return date - new Period((w - 4), TimeUnit.Days);
            }

            return date + new Period((4 - w - 7), TimeUnit.Days);
        }
    }
}
