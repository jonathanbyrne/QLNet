using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Indexes;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class FraRateHelper : RelativeDateRateHelper
    {
        private Date fixingDate_;
        private IborIndex iborIndex_;
        private Period periodToStart_;
        private Pillar.Choice pillarChoice_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public FraRateHelper(Handle<Quote> rate,
            int monthsToStart,
            int monthsToEnd,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null) :
            base(rate)
        {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
            pillarChoice_ = pillarChoice;

            Utils.QL_REQUIRE(monthsToEnd > monthsToStart, () =>
                "monthsToEnd (" + monthsToEnd + ") must be grater than monthsToStart (" + monthsToStart + ")");

            iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(double rate,
            int monthsToStart,
            int monthsToEnd,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
            pillarChoice_ = pillarChoice;

            Utils.QL_REQUIRE(monthsToEnd > monthsToStart, () =>
                "monthsToEnd (" + monthsToEnd + ") must be grater than monthsToStart (" + monthsToStart + ")");

            iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(Handle<Quote> rate,
            int monthsToStart, IborIndex i,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
            pillarChoice_ = pillarChoice;
            iborIndex_ = i.clone(termStructureHandle_);

            // We want to be notified of changes of fixings, but we don't
            // want notifications from termStructureHandle_ (they would
            // interfere with bootstrapping.)
            iborIndex_.registerWith(update);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(double rate,
            int monthsToStart,
            IborIndex i,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
            pillarChoice_ = pillarChoice;

            iborIndex_ = i.clone(termStructureHandle_);
            iborIndex_.registerWith(update);
            pillarDate_ = customPillarDate;

            initializeDates();
        }

        public FraRateHelper(Handle<Quote> rate,
            Period periodToStart,
            int lengthInMonths,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = periodToStart;
            pillarChoice_ = pillarChoice;
            // no way to take fixing into account,
            // even if we would like to for FRA over today
            iborIndex_ = new IborIndex("no-fix", // correct family name would be needed
                new Period(lengthInMonths, TimeUnit.Months),
                fixingDays,
                new Currency(), calendar, convention,
                endOfMonth, dayCounter, termStructureHandle_);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(double rate,
            Period periodToStart,
            int lengthInMonths,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = periodToStart;
            pillarChoice_ = pillarChoice;
            // no way to take fixing into account,
            // even if we would like to for FRA over today
            iborIndex_ = new IborIndex("no-fix", // correct family name would be needed
                new Period(lengthInMonths, TimeUnit.Months),
                fixingDays,
                new Currency(), calendar, convention,
                endOfMonth, dayCounter, termStructureHandle_);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(Handle<Quote> rate,
            Period periodToStart,
            IborIndex iborIndex,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = periodToStart;
            pillarChoice_ = pillarChoice;
            // no way to take fixing into account,
            // even if we would like to for FRA over today
            iborIndex_ = iborIndex.clone(termStructureHandle_);
            iborIndex_.registerWith(update);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public FraRateHelper(double rate,
            Period periodToStart,
            IborIndex iborIndex,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            periodToStart_ = periodToStart;
            pillarChoice_ = pillarChoice;
            // no way to take fixing into account,
            // even if we would like to for FRA over today
            iborIndex_ = iborIndex.clone(termStructureHandle_);
            iborIndex_.registerWith(update);
            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public override double impliedQuote()
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
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
            var spotDate = iborIndex_.fixingCalendar().advance(referenceDate, new Period(iborIndex_.fixingDays(), TimeUnit.Days));
            earliestDate_ = iborIndex_.fixingCalendar().advance(spotDate,
                periodToStart_,
                iborIndex_.businessDayConvention(),
                iborIndex_.endOfMonth());
            // maturity date is calculated from spot date
            maturityDate_ = iborIndex_.fixingCalendar().advance(spotDate,
                periodToStart_ + iborIndex_.tenor(),
                iborIndex_.businessDayConvention(),
                iborIndex_.endOfMonth());

            // latest relevant date is calculated from earliestDate_ instead
            latestRelevantDate_ = iborIndex_.maturityDate(earliestDate_);

            switch (pillarChoice_)
            {
                case Pillar.Choice.MaturityDate:
                    pillarDate_ = maturityDate_;
                    break;
                case Pillar.Choice.LastRelevantDate:
                    pillarDate_ = latestRelevantDate_;
                    break;
                case Pillar.Choice.CustomDate:
                    // pillarDate_ already assigned at construction time
                    Utils.QL_REQUIRE(pillarDate_ >= earliestDate_, () =>
                        "pillar date (" + pillarDate_ + ") must be later than or equal to the instrument's earliest date (" +
                        earliestDate_ + ")");
                    Utils.QL_REQUIRE(pillarDate_ <= latestRelevantDate_, () =>
                        "pillar date (" + pillarDate_ + ") must be before or equal to the instrument's latest relevant date (" +
                        latestRelevantDate_ + ")");
                    break;
                default:
                    Utils.QL_FAIL("unknown Pillar::Choice(" + pillarChoice_ + ")");
                    break;
            }

            latestDate_ = pillarDate_; // backward compatibility
            fixingDate_ = iborIndex_.fixingDate(earliestDate_);
        }
    }
}
