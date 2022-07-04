using System.Collections.Generic;
using QLNet.Math;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class MonthlyYieldFinder : ISolver1d
    {
        private double faceAmount_;
        private List<CashFlow> cashflows_;
        private Date settlement_;

        public MonthlyYieldFinder(double faceAmount, List<CashFlow> cashflows, Date settlement)
        {
            faceAmount_ = faceAmount;
            cashflows_ = cashflows;
            settlement_ = settlement;
        }

        public override double value(double yield) => Utils.PVDifference(faceAmount_, cashflows_, yield, settlement_);
    }
}