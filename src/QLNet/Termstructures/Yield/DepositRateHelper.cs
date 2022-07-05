using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Indexes;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class DepositRateHelper : RelativeDateRateHelper
    {
        private Date fixingDate_;
        private IborIndex iborIndex_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public DepositRateHelper(Handle<Quote> rate,
            Period tenor,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter)
            : base(rate)
        {
            iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public DepositRateHelper(double rate,
            Period tenor,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter) :
            base(rate)
        {
            iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public DepositRateHelper(Handle<Quote> rate, IborIndex i)
            : base(rate)
        {
            iborIndex_ = i.clone(termStructureHandle_);
            initializeDates();
        }

        public DepositRateHelper(double rate, IborIndex i)
            : base(rate)
        {
            iborIndex_ = i.clone(termStructureHandle_);
            initializeDates();
        }

        /////////////////////////////////////////
        //! RateHelper interface
        public override double impliedQuote()
        {
            QLNet.Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
            // the forecast fixing flag is set to true because
            // we do not want to take fixing into account
            return iborIndex_.fixing(fixingDate_, true);
        }

        public override void setTermStructure(YieldTermStructure t)
        {
            // no need to register---the index is not lazy
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        protected override void initializeDates()
        {
            // if the evaluation date is not a business day
            // then move to the next business day
            var referenceDate = iborIndex_.fixingCalendar().adjust(evaluationDate_);
            earliestDate_ = iborIndex_.valueDate(referenceDate);
            fixingDate_ = iborIndex_.fixingDate(earliestDate_);
            maturityDate_ = iborIndex_.maturityDate(earliestDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
        }
    }
}
