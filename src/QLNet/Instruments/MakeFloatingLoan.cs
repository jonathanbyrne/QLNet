using QLNet.Indexes;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class MakeFloatingLoan
    {
        private double nominal_;
        private Calendar calendar_;
        private Date startDate_, endDate_;
        private Frequency frequency_;
        private BusinessDayConvention convention_;
        private DayCounter dayCounter_;
        private double spread_;
        private Loan.Type type_;
        private Loan.Amortising amortising_;
        private DateGeneration.Rule rule_;
        private bool endOfMonth_;
        private IborIndex index_;

        public MakeFloatingLoan(Date startDate, Date endDate, double spread, Frequency frequency)
        {
            startDate_ = startDate;
            endDate_ = endDate;
            spread_ = spread;
            frequency_ = frequency;

            type_ = Loan.Type.Loan;
            amortising_ = Loan.Amortising.Bullet;
            nominal_ = 1.0;
            calendar_ = new TARGET();
            convention_ = BusinessDayConvention.ModifiedFollowing;
            dayCounter_ = new Actual365Fixed();
            rule_ = DateGeneration.Rule.Forward;
            endOfMonth_ = false;
            index_ = new IborIndex();
        }

        public MakeFloatingLoan withType(Loan.Type type)
        {
            type_ = type;
            return this;
        }

        public MakeFloatingLoan withNominal(double n)
        {
            nominal_ = n;
            return this;
        }

        public MakeFloatingLoan withCalendar(Calendar c)
        {
            calendar_ = c;
            return this;
        }

        public MakeFloatingLoan withConvention(BusinessDayConvention bdc)
        {
            convention_ = bdc;
            return this;
        }

        public MakeFloatingLoan withDayCounter(DayCounter dc)
        {
            dayCounter_ = dc;
            return this;
        }

        public MakeFloatingLoan withRule(DateGeneration.Rule r)
        {
            rule_ = r;
            return this;
        }

        public MakeFloatingLoan withEndOfMonth(bool flag)
        {
            endOfMonth_ = flag;
            return this;
        }

        public MakeFloatingLoan withAmortising(Loan.Amortising Amortising)
        {
            amortising_ = Amortising;
            return this;
        }

        public MakeFloatingLoan withIndex(IborIndex index)
        {
            index_ = index;
            return this;
        }

        // Loan creator
        public static implicit operator FloatingLoan(MakeFloatingLoan o) => o.value();

        public FloatingLoan value()
        {

            var floatingSchedule = new Schedule(startDate_, endDate_, new Period(frequency_),
                calendar_, convention_, convention_, rule_, endOfMonth_);

            var principalPeriod = amortising_ == Loan.Amortising.Bullet ?
                new Period(Frequency.Once) :
                new Period(frequency_);

            var principalSchedule = new Schedule(startDate_, endDate_, principalPeriod,
                calendar_, convention_, convention_, rule_, endOfMonth_);

            var fl = new FloatingLoan(type_, nominal_, floatingSchedule, spread_, dayCounter_,
                principalSchedule, convention_, index_);
            return fl;

        }

    }
}