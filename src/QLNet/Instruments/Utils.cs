using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments
{
    public static partial class Utils
    {
        public static List<double> CreateCouponSchedule(Schedule schedule,
            CouponConversionSchedule couponConversionSchedule)
        {
            List<double> ret = new InitializedList<double>(schedule.Count);
            for (var i = 0; i < couponConversionSchedule.Count; i++)
            for (var j = 0; j < schedule.Count; j++)
            {
                if (schedule[j] >= (Date)couponConversionSchedule[i].Date)
                {
                    ret[j] = couponConversionSchedule[i].Rate;
                }
            }

            return ret;
        }
    }
}
