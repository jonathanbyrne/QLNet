using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class VoluntaryPrepay : SimpleCashFlow
    {
        public VoluntaryPrepay(double amount, Date date) : base(amount, date)
        {
        }
    }
}
