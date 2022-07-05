using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class AmortizingPayment : SimpleCashFlow
    {
        public AmortizingPayment(double amount, Date date) : base(amount, date)
        {
        }
    }
}
