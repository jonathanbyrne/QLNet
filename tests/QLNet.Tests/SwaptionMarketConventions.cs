using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests;

public struct SwaptionMarketConventions
{
    public Calendar calendar;
    public BusinessDayConvention optionBdc;
    public DayCounter dayCounter;
    public void setConventions()
    {
        calendar = new TARGET();
        optionBdc = BusinessDayConvention.ModifiedFollowing;
        dayCounter = new Actual365Fixed();
    }
}