using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class Redemption : SimpleCashFlow
    {
        public Redemption(double amount, Date date) : base(amount, date) { }
    }
}