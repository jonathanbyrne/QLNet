using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class FxSwapRateHelper : RelativeDateRateHelper
    {
        private Calendar cal_;
        private Handle<YieldTermStructure> collHandle_;
        private RelinkableHandle<YieldTermStructure> collRelinkableHandle_ = new RelinkableHandle<YieldTermStructure>();
        private BusinessDayConvention conv_;
        private bool eom_;
        private int fixingDays_;
        private bool isFxBaseCurrencyCollateralCurrency_;
        private Handle<Quote> spot_;
        private Period tenor_;
        private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        public FxSwapRateHelper(Handle<Quote> fwdPoint,
            Handle<Quote> spotFx,
            Period tenor,
            int fixingDays,
            Calendar calendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            bool isFxBaseCurrencyCollateralCurrency,
            Handle<YieldTermStructure> coll)
            : base(fwdPoint)
        {
            spot_ = spotFx;
            tenor_ = tenor;
            fixingDays_ = fixingDays;
            cal_ = calendar;
            conv_ = convention;
            eom_ = endOfMonth;
            isFxBaseCurrencyCollateralCurrency_ = isFxBaseCurrencyCollateralCurrency;
            collHandle_ = coll;

            spot_.registerWith(update);
            collHandle_.registerWith(update);
            initializeDates();
        }

        public BusinessDayConvention businessDayConvention() => conv_;

        public Calendar calendar() => cal_;

        public bool endOfMonth() => eom_;

        public int fixingDays() => fixingDays_;

        // RateHelper interface
        public override double impliedQuote()
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");

            Utils.QL_REQUIRE(!collHandle_.empty(), () => "collateral term structure not set");

            var d1 = collHandle_.link.discount(earliestDate_);
            var d2 = collHandle_.link.discount(latestDate_);
            var collRatio = d1 / d2;
            d1 = termStructureHandle_.link.discount(earliestDate_);
            d2 = termStructureHandle_.link.discount(latestDate_);
            var ratio = d1 / d2;
            var spot = spot_.link.value();
            if (isFxBaseCurrencyCollateralCurrency_)
            {
                return (ratio / collRatio - 1) * spot;
            }

            return (collRatio / ratio - 1) * spot;
        }

        public bool isFxBaseCurrencyCollateralCurrency() => isFxBaseCurrencyCollateralCurrency_;

        public override void setTermStructure(YieldTermStructure t)
        {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed

            termStructureHandle_.linkTo(t, false);
            collRelinkableHandle_.linkTo(collHandle_, false);
            base.setTermStructure(t);
        }

        // FxSwapRateHelper inspectors
        public double spot() => spot_.link.value();

        public Period tenor() => tenor_;

        protected override void initializeDates()
        {
            // if the evaluation date is not a business day
            // then move to the next business day
            var refDate = cal_.adjust(evaluationDate_);
            earliestDate_ = cal_.advance(refDate, new Period(fixingDays_, TimeUnit.Days));
            latestDate_ = cal_.advance(earliestDate_, tenor_, conv_, eom_);
        }
    }
}
