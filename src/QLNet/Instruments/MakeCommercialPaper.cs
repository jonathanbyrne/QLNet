using JetBrains.Annotations;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class MakeCommercialPaper
    {
        private Loan.Amortising amortising_;
        private Calendar calendar_;
        private BusinessDayConvention convention_;
        private DayCounter dayCounter_;
        private bool endOfMonth_;
        private double fixedRate_;
        private Frequency frequency_;
        private double nominal_;
        private DateGeneration.Rule rule_;
        private Date startDate_, endDate_;
        private Loan.Type type_;

        public MakeCommercialPaper(Date startDate, Date endDate, double fixedRate, Frequency frequency)
        {
            startDate_ = startDate;
            endDate_ = endDate;
            fixedRate_ = fixedRate;
            frequency_ = frequency;

            type_ = Loan.Type.Loan;
            amortising_ = Loan.Amortising.Bullet;
            nominal_ = 1.0;
            calendar_ = new TARGET();
            convention_ = BusinessDayConvention.ModifiedFollowing;
            dayCounter_ = new Actual365Fixed();
            rule_ = DateGeneration.Rule.Forward;
            endOfMonth_ = false;
        }

        // Loan creator
        public static implicit operator CommercialPaper(MakeCommercialPaper o) => o.value();

        public CommercialPaper value()
        {
            var fixedSchedule = new Schedule(startDate_, endDate_, new Period(frequency_),
                calendar_, convention_, convention_, rule_, endOfMonth_);

            var principalPeriod = amortising_ == Loan.Amortising.Bullet ? new Period(Frequency.Once) : new Period(frequency_);

            var principalSchedule = new Schedule(startDate_, endDate_, principalPeriod,
                calendar_, convention_, convention_, rule_, endOfMonth_);

            var fl = new CommercialPaper(type_, nominal_, fixedSchedule, fixedRate_, dayCounter_,
                principalSchedule, convention_);
            return fl;
        }

        public MakeCommercialPaper withAmortising(Loan.Amortising Amortising)
        {
            amortising_ = Amortising;
            return this;
        }

        public MakeCommercialPaper withCalendar(Calendar c)
        {
            calendar_ = c;
            return this;
        }

        public MakeCommercialPaper withConvention(BusinessDayConvention bdc)
        {
            convention_ = bdc;
            return this;
        }

        public MakeCommercialPaper withDayCounter(DayCounter dc)
        {
            dayCounter_ = dc;
            return this;
        }

        public MakeCommercialPaper withEndOfMonth(bool flag)
        {
            endOfMonth_ = flag;
            return this;
        }

        public MakeCommercialPaper withNominal(double n)
        {
            nominal_ = n;
            return this;
        }

        public MakeCommercialPaper withRule(DateGeneration.Rule r)
        {
            rule_ = r;
            return this;
        }

        public MakeCommercialPaper withType(Loan.Type type)
        {
            type_ = type;
            return this;
        }
    }
}
