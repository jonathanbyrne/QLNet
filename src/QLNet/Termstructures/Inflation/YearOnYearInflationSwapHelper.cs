using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Pricingengines.Swap;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YearOnYearInflationSwapHelper : BootstrapHelper<YoYInflationTermStructure>
    {
        public YearOnYearInflationSwapHelper(Handle<Quote> quote,
            Period swapObsLag,
            Date maturity,
            Calendar calendar,
            BusinessDayConvention paymentConvention,
            DayCounter dayCounter,
            YoYInflationIndex yii)
            : base(quote)
        {
            swapObsLag_ = swapObsLag;
            maturity_ = maturity;
            calendar_ = calendar;
            paymentConvention_ = paymentConvention;
            dayCounter_ = dayCounter;
            yii_ = yii;

            if (yii_.interpolated())
            {
                // if interpolated then simple
                earliestDate_ = maturity_ - swapObsLag_;
                latestDate_ = maturity_ - swapObsLag_;
            }
            else
            {
                // but if NOT interpolated then the value is valid
                // for every day in an inflation period so you actually
                // get an extended validity, however for curve building
                // just put the first date because using that convention
                // for the base date throughout
                var limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                    yii_.frequency());
                earliestDate_ = limStart.Key;
                latestDate_ = limStart.Key;
            }

            // check that the observation lag of the swap
            // is compatible with the availability lag of the index AND
            // it's interpolation (assuming the start day is spot)
            if (yii_.interpolated())
            {
                var pShift = new Period(yii_.frequency());
                Utils.QL_REQUIRE(swapObsLag_ - pShift > yii_.availabilityLag(), () =>
                    "inconsistency between swap observation of index "
                    + swapObsLag_ +
                    " index availability " + yii_.availabilityLag() +
                    " index period " + pShift +
                    " and index availability " + yii_.availabilityLag() +
                    " need (obsLag-index period) > availLag");
            }

            Settings.registerWith(update);
        }

        public override void setTermStructure(YoYInflationTermStructure y)
        {
            base.setTermStructure(y);

            // set up a new YYIIS
            // but this one does NOT own its inflation term structure
            const bool own = false;

            // The effect of the new inflation term structure is
            // felt via the effect on the inflation index
            var yyts = new Handle<YoYInflationTermStructure>(y, own);

            var new_yii = yii_.clone(yyts);

            // always works because tenor is always 1 year so
            // no problem with different days-in-month
            var from = Settings.evaluationDate();
            var to = maturity_;
            var fixedSchedule = new MakeSchedule().from(from).to(to)
                .withTenor(new Period(1, TimeUnit.Years))
                .withConvention(BusinessDayConvention.Unadjusted)
                .withCalendar(calendar_)// fixed leg gets cal from sched
                .value();
            var yoySchedule = fixedSchedule;
            var spread = 0.0;
            var fixedRate = quote().link.value();

            var nominal = 1000000.0;   // has to be something but doesn't matter what
            yyiis_ = new YearOnYearInflationSwap(YearOnYearInflationSwap.Type.Payer,
                nominal,
                fixedSchedule,
                fixedRate,
                dayCounter_,
                yoySchedule,
                new_yii,
                swapObsLag_,
                spread,
                dayCounter_,
                calendar_,  // inflation index does not have a calendar
                paymentConvention_);


            // Because very simple instrument only takes
            // standard discounting swap engine.
            yyiis_.setPricingEngine(new DiscountingSwapEngine(y.nominalTermStructure()));
        }

        public override double impliedQuote()
        {
            // what does the term structure imply?
            // in this case just the same value ... trivial case
            // (would not be so for an inflation-linked bond)
            yyiis_.recalculate();
            return yyiis_.fairRate();
        }

        protected Period swapObsLag_;
        protected Date maturity_;
        protected Calendar calendar_;
        protected BusinessDayConvention paymentConvention_;
        protected DayCounter dayCounter_;
        protected YoYInflationIndex yii_;
        protected YearOnYearInflationSwap yyiis_;
    }
}