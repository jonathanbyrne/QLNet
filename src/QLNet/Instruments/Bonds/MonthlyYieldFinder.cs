using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class MonthlyYieldFinder : ISolver1d
    {
        private List<CashFlow> cashflows_;
        private double faceAmount_;
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
