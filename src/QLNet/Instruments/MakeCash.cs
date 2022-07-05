using JetBrains.Annotations;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class MakeCash
    {
        private Loan.Amortising amortising_;
        private Calendar calendar_;
        private BusinessDayConvention convention_;
        private DayCounter dayCounter_;
        private bool endOfMonth_;
        private Frequency frequency_;
        private double nominal_;
        private DateGeneration.Rule rule_;
        private Date startDate_, endDate_;
        private Loan.Type type_;

        public MakeCash(Date startDate, Date endDate, double nominal)
        {
            startDate_ = startDate;
            endDate_ = endDate;
            nominal_ = nominal;

            frequency_ = Frequency.Once;
            type_ = Loan.Type.Loan;
            amortising_ = Loan.Amortising.Bullet;
            calendar_ = new TARGET();
            convention_ = BusinessDayConvention.ModifiedFollowing;
            dayCounter_ = new Actual365Fixed();
            rule_ = DateGeneration.Rule.Forward;
            endOfMonth_ = false;
        }

        // Loan creator
        public static implicit operator Cash(MakeCash o) => o.value();

        public Cash value()
        {
            var principalPeriod = amortising_ == Loan.Amortising.Bullet ? new Period(Frequency.Once) : new Period(frequency_);

            var principalSchedule = new Schedule(startDate_, endDate_, principalPeriod,
                calendar_, convention_, convention_, rule_, endOfMonth_);

            var c = new Cash(type_, nominal_, principalSchedule, convention_);
            return c;
        }

        public MakeCash withAmortising(Loan.Amortising Amortising)
        {
            amortising_ = Amortising;
            return this;
        }

        public MakeCash withCalendar(Calendar c)
        {
            calendar_ = c;
            return this;
        }

        public MakeCash withConvention(BusinessDayConvention bdc)
        {
            convention_ = bdc;
            return this;
        }

        public MakeCash withDayCounter(DayCounter dc)
        {
            dayCounter_ = dc;
            return this;
        }

        public MakeCash withEndOfMonth(bool flag)
        {
            endOfMonth_ = flag;
            return this;
        }

        public MakeCash withRule(DateGeneration.Rule r)
        {
            rule_ = r;
            return this;
        }

        public MakeCash withType(Loan.Type type)
        {
            type_ = type;
            return this;
        }
    }
}
