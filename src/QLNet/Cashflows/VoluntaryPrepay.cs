using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class VoluntaryPrepay : SimpleCashFlow
    {
        public VoluntaryPrepay(double amount, Date date) : base(amount, date) { }
    }
}