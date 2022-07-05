using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Pricingengines.Swap;
using QLNet.Quotes;
using QLNet.Time;
using QLNet.Time.Calendars;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class BMASwapRateHelper : RelativeDateRateHelper
    {
        protected BusinessDayConvention bmaConvention_;
        protected DayCounter bmaDayCount_;
        protected BMAIndex bmaIndex_;
        protected Period bmaPeriod_;
        protected Calendar calendar_;
        protected IborIndex iborIndex_;
        protected int settlementDays_;
        protected BMASwap swap_;
        protected Period tenor_;
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public BMASwapRateHelper(Handle<Quote> liborFraction,
            Period tenor,
            int settlementDays,
            Calendar calendar,
            Period bmaPeriod,
            BusinessDayConvention bmaConvention,
            DayCounter bmaDayCount,
            BMAIndex bmaIndex,
            IborIndex iborIndex)
            : base(liborFraction)
        {
            tenor_ = tenor;
            settlementDays_ = settlementDays;
            calendar_ = calendar;
            bmaPeriod_ = bmaPeriod;
            bmaConvention_ = bmaConvention;
            bmaDayCount_ = bmaDayCount;
            bmaIndex_ = bmaIndex;
            iborIndex_ = iborIndex;

            iborIndex_.registerWith(update);
            bmaIndex_.registerWith(update);

            initializeDates();
        }

        // RateHelper interface
        public override double impliedQuote()
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
            // we didn't register as observers - force calculation
            swap_.recalculate();
            return swap_.fairLiborFraction();
        }

        public override void setTermStructure(YieldTermStructure t)
        {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        protected override void initializeDates()
        {
            // if the evaluation date is not a business day
            // then move to the next business day
            var jc = new JointCalendar(calendar_, iborIndex_.fixingCalendar());
            var referenceDate = jc.adjust(evaluationDate_);
            earliestDate_ = calendar_.advance(referenceDate, new Period(settlementDays_, TimeUnit.Days));

            var maturity = earliestDate_ + tenor_;

            // dummy BMA index with curve/swap arguments
            var clonedIndex = new BMAIndex(termStructureHandle_);

            var bmaSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
                .withTenor(bmaPeriod_)
                .withCalendar(bmaIndex_.fixingCalendar())
                .withConvention(bmaConvention_)
                .backwards().value();

            var liborSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
                .withTenor(iborIndex_.tenor())
                .withCalendar(iborIndex_.fixingCalendar())
                .withConvention(iborIndex_.businessDayConvention())
                .endOfMonth(iborIndex_.endOfMonth())
                .backwards().value();

            swap_ = new BMASwap(BMASwap.Type.Payer,
                100.0, liborSchedule, 0.75, // arbitrary
                0.0, iborIndex_, iborIndex_.dayCounter(), bmaSchedule, clonedIndex, bmaDayCount_);

            swap_.setPricingEngine(new DiscountingSwapEngine(iborIndex_.forwardingTermStructure()));

            var d = calendar_.adjust(swap_.maturityDate());
            var w = d.weekday();
            var nextWednesday = w >= 4 ? d + new Period(11 - w, TimeUnit.Days) : d + new Period(4 - w, TimeUnit.Days);
            latestDate_ = clonedIndex.valueDate(clonedIndex.fixingCalendar().adjust(nextWednesday));
        }
    }
}
