using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class Redemption : SimpleCashFlow
    {
        public Redemption(double amount, Date date) : base(amount, date)
        {
        }
    }
}
