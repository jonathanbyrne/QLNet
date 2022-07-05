using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class FixedDividend : Dividend
    {
        protected double amount_;

        public FixedDividend(double amount, Date date)
            : base(date)
        {
            amount_ = amount;
        }

        public override double amount() => amount_;

        public override double amount(double d) => amount_;
    }
}
