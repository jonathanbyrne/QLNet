using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    /// <summary>
    ///     Overnight CAD Libor index
    /// </summary>
    [PublicAPI]
    public class CADLiborON : DailyTenorLibor
    {
        public CADLiborON()
            : base("CADLibor", 0, new CADCurrency(), new Canada(), new Actual365Fixed(), new Handle<YieldTermStructure>())
        {
        }

        public CADLiborON(Handle<YieldTermStructure> h)
            : base("CADLibor", 0, new CADCurrency(), new Canada(), new Actual365Fixed(), h)
        {
        }
    }
}
