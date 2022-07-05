using System;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class CouponConversion
    {
        public CouponConversion(DateTime date, double rate)
        {
            Date = date;
            Rate = rate;
        }

        public DateTime Date { get; set; }

        public double Rate { get; set; }

        public override string ToString() => ($"Conversion Date : {Date}\nConversion Rate : {Rate}");
    }
}
