using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class FixedDividend : Dividend
    {
        protected double amount_;
        public override double amount() => amount_;

        public override double amount(double d) => amount_;

        public FixedDividend(double amount, Date date)
            : base(date)
        {
            amount_ = amount;
        }
    }
}