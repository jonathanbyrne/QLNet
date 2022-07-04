using QLNet.Time;

namespace QLNet
{
    public partial class Utils
    {
        public static Date previousWednesday(Date date)
        {
            var w = date.weekday();
            if (w >= 4)   // roll back w-4 days
                return date - new Period((w - 4), TimeUnit.Days);
            else // roll forward 4-w days and back one week
                return date + new Period((4 - w - 7), TimeUnit.Days);
        }

        public static Date nextWednesday(Date date) => previousWednesday(date + 7);
    }
}