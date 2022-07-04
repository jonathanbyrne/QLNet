using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class AmortizingPayment : SimpleCashFlow
    {
        public AmortizingPayment(double amount, Date date) : base(amount, date) { }
    }
}