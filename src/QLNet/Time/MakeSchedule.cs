using QLNet.Time.Calendars;

namespace QLNet.Time
{
    /// <summary>
    /// This class provides a more comfortable interface to the argument list of Schedule's constructor.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class MakeSchedule
    {
        public MakeSchedule() { rule_ = DateGeneration.Rule.Backward; endOfMonth_ = false; }

        public MakeSchedule from(Date effectiveDate)
        {
            effectiveDate_ = effectiveDate;
            return this;
        }

        public MakeSchedule to(Date terminationDate)
        {
            terminationDate_ = terminationDate;
            return this;
        }

        public MakeSchedule withTenor(Period tenor)
        {
            tenor_ = tenor;
            return this;
        }

        public MakeSchedule withFrequency(Frequency frequency)
        {
            tenor_ = new Period(frequency);
            return this;
        }

        public MakeSchedule withCalendar(Calendar calendar)
        {
            calendar_ = calendar;
            return this;
        }

        public MakeSchedule withConvention(BusinessDayConvention conv)
        {
            convention_ = conv;
            return this;
        }

        public MakeSchedule withTerminationDateConvention(BusinessDayConvention conv)
        {
            terminationDateConvention_ = conv;
            return this;
        }

        public MakeSchedule withRule(DateGeneration.Rule r)
        {
            rule_ = r;
            return this;
        }

        public MakeSchedule forwards()
        {
            rule_ = DateGeneration.Rule.Forward;
            return this;
        }

        public MakeSchedule backwards()
        {
            rule_ = DateGeneration.Rule.Backward;
            return this;
        }

        public MakeSchedule endOfMonth(bool flag = true)
        {
            endOfMonth_ = flag;
            return this;
        }

        public MakeSchedule withFirstDate(Date d)
        {
            firstDate_ = d;
            return this;
        }

        public MakeSchedule withNextToLastDate(Date d)
        {
            nextToLastDate_ = d;
            return this;
        }

        public Schedule value()
        {

            // check for mandatory arguments
            Utils.QL_REQUIRE(effectiveDate_ != null, () => "effective date not provided");
            Utils.QL_REQUIRE(terminationDate_ != null, () => "termination date not provided");
            Utils.QL_REQUIRE((object)tenor_ != null, () => "tenor/frequency not provided");

            // if no calendar was set...
            if (calendar_ == null)
            {
                // ...we use a null one.
                calendar_ = new NullCalendar();
            }

            // set dynamic defaults:
            BusinessDayConvention convention;
            // if a convention was set, we use it.
            if (convention_ != null)
            {
                convention = convention_.Value;
            }
            else
            {
                if (!calendar_.empty())
                {
                    // ...if we set a calendar, we probably want it to be used;
                    convention = BusinessDayConvention.Following;
                }
                else
                {
                    // if not, we don't care.
                    convention = BusinessDayConvention.Unadjusted;
                }
            }

            BusinessDayConvention terminationDateConvention;
            // if set explicitly, we use it;
            if (terminationDateConvention_ != null)
            {
                terminationDateConvention = terminationDateConvention_.Value;
            }
            else
            {
                // Unadjusted as per ISDA specification
                terminationDateConvention = convention;
            }

            return new Schedule(effectiveDate_, terminationDate_, tenor_, calendar_,
                convention, terminationDateConvention,
                rule_, endOfMonth_, firstDate_, nextToLastDate_);
        }

        private Calendar calendar_;
        private Date effectiveDate_, terminationDate_;
        private Period tenor_;
        private BusinessDayConvention? convention_, terminationDateConvention_;
        private DateGeneration.Rule rule_;
        private bool endOfMonth_;
        private Date firstDate_, nextToLastDate_;
    }
}