using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class SwapRateHelper : RelativeDateRateHelper
    {
        protected Calendar calendar_;
        protected Handle<YieldTermStructure> discountHandle_;
        protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_ = new RelinkableHandle<YieldTermStructure>();
        protected BusinessDayConvention fixedConvention_;
        protected DayCounter fixedDayCount_;
        protected Frequency fixedFrequency_;
        protected Period fwdStart_;
        protected IborIndex iborIndex_;
        protected Pillar.Choice pillarChoice_;
        protected int? settlementDays_;
        protected Handle<Quote> spread_;
        protected VanillaSwap swap_;
        protected Period tenor_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public SwapRateHelper(Handle<Quote> rate,
            SwapIndex swapIndex,
            Handle<Quote> spread = null,
            Period fwdStart = null,
            // exogenous discounting curve
            Handle<YieldTermStructure> discount = null,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            spread_ = spread ?? new Handle<Quote>();
            fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);

            settlementDays_ = swapIndex.fixingDays();
            tenor_ = swapIndex.tenor();
            pillarChoice_ = pillarChoice;
            calendar_ = swapIndex.fixingCalendar();
            fixedConvention_ = swapIndex.fixedLegConvention();
            fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
            fixedDayCount_ = swapIndex.dayCounter();
            iborIndex_ = swapIndex.iborIndex();
            fwdStart_ = fwdStart;
            discountHandle_ = discount ?? new Handle<YieldTermStructure>();

            // take fixing into account
            iborIndex_ = swapIndex.iborIndex().clone(termStructureHandle_);
            // We want to be notified of changes of fixings, but we don't
            // want notifications from termStructureHandle_ (they would
            // interfere with bootstrapping.)
            iborIndex_.registerWith(update);
            spread_.registerWith(update);
            discountHandle_.registerWith(update);
            pillarDate_ = customPillarDate;

            initializeDates();
        }

        public SwapRateHelper(Handle<Quote> rate,
            Period tenor,
            Calendar calendar,
            Frequency fixedFrequency,
            BusinessDayConvention fixedConvention,
            DayCounter fixedDayCount,
            IborIndex iborIndex,
            Handle<Quote> spread = null,
            Period fwdStart = null,
            // exogenous discounting curve
            Handle<YieldTermStructure> discount = null,
            int? settlementDays = null,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            settlementDays_ = settlementDays;
            tenor_ = tenor;
            pillarChoice_ = pillarChoice;
            calendar_ = calendar;
            fixedConvention_ = fixedConvention;
            fixedFrequency_ = fixedFrequency;
            fixedDayCount_ = fixedDayCount;
            spread_ = spread ?? new Handle<Quote>();
            fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
            discountHandle_ = discount ?? new Handle<YieldTermStructure>();

            if (settlementDays_ == null)
            {
                settlementDays_ = iborIndex.fixingDays();
            }

            // take fixing into account
            iborIndex_ = iborIndex.clone(termStructureHandle_);
            // We want to be notified of changes of fixings, but we don't
            // want notifications from termStructureHandle_ (they would
            // interfere with bootstrapping.)
            iborIndex_.registerWith(update);
            spread_.registerWith(update);
            discountHandle_.registerWith(update);

            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public SwapRateHelper(double rate,
            SwapIndex swapIndex,
            Handle<Quote> spread = null,
            Period fwdStart = null,
            // exogenous discounting curve
            Handle<YieldTermStructure> discount = null,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            settlementDays_ = swapIndex.fixingDays();
            tenor_ = swapIndex.tenor();
            pillarChoice_ = pillarChoice;
            calendar_ = swapIndex.fixingCalendar();
            fixedConvention_ = swapIndex.fixedLegConvention();
            fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
            fixedDayCount_ = swapIndex.dayCounter();
            spread_ = spread ?? new Handle<Quote>();
            fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
            discountHandle_ = discount ?? new Handle<YieldTermStructure>();

            // take fixing into account
            iborIndex_ = swapIndex.iborIndex().clone(termStructureHandle_);
            // We want to be notified of changes of fixings, but we don't
            // want notifications from termStructureHandle_ (they would
            // interfere with bootstrapping.)
            iborIndex_.registerWith(update);
            spread_.registerWith(update);
            discountHandle_.registerWith(update);

            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public SwapRateHelper(double rate,
            Period tenor,
            Calendar calendar,
            Frequency fixedFrequency,
            BusinessDayConvention fixedConvention,
            DayCounter fixedDayCount,
            IborIndex iborIndex,
            Handle<Quote> spread = null,
            Period fwdStart = null,
            // exogenous discounting curve
            Handle<YieldTermStructure> discount = null,
            int? settlementDays = null,
            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
            Date customPillarDate = null)
            : base(rate)
        {
            settlementDays_ = settlementDays;
            tenor_ = tenor;
            pillarChoice_ = pillarChoice;
            calendar_ = calendar;
            fixedConvention_ = fixedConvention;
            fixedFrequency_ = fixedFrequency;
            fixedDayCount_ = fixedDayCount;
            spread_ = spread ?? new Handle<Quote>();
            fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
            discountHandle_ = discount ?? new Handle<YieldTermStructure>();

            if (settlementDays_ == null)
            {
                settlementDays_ = iborIndex.fixingDays();
            }

            // take fixing into account
            iborIndex_ = iborIndex.clone(termStructureHandle_);
            // We want to be notified of changes of fixings, but we don't
            // want notifications from termStructureHandle_ (they would
            // interfere with bootstrapping.)
            iborIndex_.registerWith(update);
            spread_.registerWith(update);
            discountHandle_.registerWith(update);

            pillarDate_ = customPillarDate;
            initializeDates();
        }

        public Period forwardStart() => fwdStart_;

        public override double impliedQuote()
        {
            QLNet.Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
            // we didn't register as observers - force calculation
            swap_.recalculate(); // it is from lazy objects
            // weak implementation... to be improved
            var floatingLegNPV = swap_.floatingLegNPV();
            var spread = this.spread();
            var spreadNPV = swap_.floatingLegBPS() / Const.BASIS_POINT * spread;
            var totNPV = -(floatingLegNPV + spreadNPV);
            var result = totNPV / (swap_.fixedLegBPS() / Const.BASIS_POINT);
            return result;
        }

        public override void setTermStructure(YieldTermStructure t)
        {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
            discountRelinkableHandle_.linkTo(discountHandle_.empty() ? t : discountHandle_, false);
        }

        // SwapRateHelper inspectors
        public double spread() => spread_.empty() ? 0.0 : spread_.link.value();

        public VanillaSwap swap() => swap_;

        protected override void initializeDates()
        {
            // do not pass the spread here, as it might be a Quote i.e. it can dinamically change
            // input discount curve Handle might be empty now but it could be assigned a curve later;
            // use a RelinkableHandle here
            swap_ = new MakeVanillaSwap(tenor_, iborIndex_, 0.0, fwdStart_)
                .withSettlementDays(settlementDays_.Value)
                .withDiscountingTermStructure(discountRelinkableHandle_)
                .withFixedLegDayCount(fixedDayCount_)
                .withFixedLegTenor(new Period(fixedFrequency_))
                .withFixedLegConvention(fixedConvention_)
                .withFixedLegTerminationDateConvention(fixedConvention_)
                .withFixedLegCalendar(calendar_)
                .withFloatingLegCalendar(calendar_);

            earliestDate_ = swap_.startDate();

            // Usually...
            maturityDate_ = latestRelevantDate_ = swap_.maturityDate();

            // ...but due to adjustments, the last floating coupon might
            // need a later date for fixing
#if QL_USE_INDEXED_COUPON
         FloatingRateCoupon lastCoupon = (FloatingRateCoupon)swap_.floatingLeg()[swap_.floatingLeg().Count - 1];
         Date fixingValueDate = iborIndex_.valueDate(lastFloating.fixingDate());
         Date endValueDate = iborIndex_.maturityDate(fixingValueDate);
         latestDate_ = Date.Max(latestDate_, endValueDate);
#endif

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
                    QLNet.Utils.QL_REQUIRE(pillarDate_ >= earliestDate_, () =>
                        "pillar date (" + pillarDate_ + ") must be later " +
                        "than or equal to the instrument's earliest date (" +
                        earliestDate_ + ")");
                    QLNet.Utils.QL_REQUIRE(pillarDate_ <= latestRelevantDate_, () =>
                        "pillar date (" + pillarDate_ + ") must be before " +
                        "or equal to the instrument's latest relevant date (" +
                        latestRelevantDate_ + ")");
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown Pillar::Choice(" + pillarChoice_ + ")");
                    break;
            }

            latestDate_ = pillarDate_; // backward compatibility
        }
    }
}
