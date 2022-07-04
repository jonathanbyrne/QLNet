using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class FractionalDividend : Dividend
    {
        protected double rate_;
        public double rate() => rate_;

        protected double? nominal_;
        public double? nominal() => nominal_;

        public FractionalDividend(double rate, Date date)
            : base(date)
        {
            rate_ = rate;
            nominal_ = null;
        }

        public FractionalDividend(double rate, double nominal, Date date)
            : base(date)
        {
            rate_ = rate;
            nominal_ = nominal;
        }

        // Dividend interface
        public override double amount()
        {
            Utils.QL_REQUIRE(nominal_ != null, () => "no nominal given");
            return rate_ * nominal_.GetValueOrDefault();
        }

        public override double amount(double underlying) => rate_ * underlying;
    }
}