using QLNet.Time;

namespace QLNet
{
    public partial class Utils
    {
        public static Date time2Date(Date referenceDate, DayCounter dc, double t)
        {
            t -= 1e4 * Const.QL_EPSILON; // add a small buffer for rounding errors
            var d = new Date(referenceDate);
            while (dc.yearFraction(referenceDate, d += new Period(1, TimeUnit.Years)) < t)
            {
                ;
            }

            d -= new Period(1, TimeUnit.Years);
            while (dc.yearFraction(referenceDate, d += new Period(1, TimeUnit.Months)) < t)
            {
                ;
            }

            d -= new Period(1, TimeUnit.Months);
            while (dc.yearFraction(referenceDate, d++) < t)
            {
                ;
            }

            return d;
        }
    }
}
