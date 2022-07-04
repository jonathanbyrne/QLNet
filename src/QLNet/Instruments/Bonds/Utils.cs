using System.Collections.Generic;
using QLNet.Time;

namespace QLNet
{
    public partial class Utils
    {
        public static double PVDifference(double faceAmount, List<CashFlow> cashflows, double yield, Date settlement)
        {
            var price = 0.0;
            var actualDate = new Date(1, 1, 1970) ;
            var cashflowindex = 0 ;


            for (var i = 0; i < cashflows.Count; i++)
            {
                if (cashflows[i].hasOccurred(settlement))
                    continue;
                // TODO use daycounter to find cashflowindex
                if (cashflows[i].date() != actualDate)
                {
                    actualDate = cashflows[i].date();
                    cashflowindex++;
                }
                var amount = cashflows[i].amount();
                price += amount / System.Math.Pow((1 + yield / 100), cashflowindex);
            }

            return price - faceAmount;


        }
    }
}