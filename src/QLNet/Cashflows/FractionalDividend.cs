using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class FractionalDividend : Dividend
    {
        protected double? nominal_;
        protected double rate_;

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
            QLNet.Utils.QL_REQUIRE(nominal_ != null, () => "no nominal given");
            return rate_ * nominal_.GetValueOrDefault();
        }

        public override double amount(double underlying) => rate_ * underlying;

        public double? nominal() => nominal_;

        public double rate() => rate_;
    }
}
