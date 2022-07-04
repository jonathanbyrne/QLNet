using System.Collections.Generic;
using QLNet.Time;

namespace QLNet
{
    public static partial class Utils
    {
        //! utility function giving the inflation period for a given date
        public static KeyValuePair<Date, Date> inflationPeriod(Date d, Frequency frequency)
        {
            var month = (Month) d.Month;
            var year = d.Year;

            Month startMonth = 0;
            Month endMonth = 0;
            switch (frequency)
            {
                case Frequency.Annual:
                    startMonth = Month.January;
                    endMonth = Month.December;
                    break;
                case Frequency.Semiannual:
                    startMonth = (Month)(6 * ((int) month - 1) / 6 + 1);
                    endMonth = startMonth + 5;
                    break;
                case Frequency.Quarterly:
                    startMonth = (Month)(3 * ((int) month - 1) / 3 + 1);
                    endMonth = startMonth + 2;
                    break;
                case Frequency.Monthly:
                    startMonth = endMonth = month;
                    break;
                default:
                    Utils.QL_FAIL("Frequency not handled: " + frequency);
                    break;
            }

            var startDate = new Date(1, startMonth, year);
            var endDate = Date.endOfMonth(new Date(1, endMonth, year));

            return new KeyValuePair<Date, Date>(startDate, endDate);
        }

        public static double inflationYearFraction(Frequency f, bool indexIsInterpolated,
            DayCounter dayCounter,
            Date d1, Date d2)
        {
            double t = 0;
            if (indexIsInterpolated)
            {
                // N.B. we do not use linear interpolation between flat
                // fixing forecasts for forecasts.  This avoids awkwardnesses
                // when bootstrapping the inflation curve.
                t = dayCounter.yearFraction(d1, d2);
            }
            else
            {
                // I.e. fixing is constant for the whole inflation period.
                // Use the value for half way along the period.
                // But the inflation time is the time between period starts
                var limD1 = inflationPeriod(d1, f);
                var limD2 = inflationPeriod(d2, f);
                t = dayCounter.yearFraction(limD1.Key, limD2.Key);
            }
            return t;
        }
    }
}