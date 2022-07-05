using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class YoYInflationIndex : InflationIndex
    {
        private bool ratio_;
        private Handle<YoYInflationTermStructure> yoyInflation_;

        public YoYInflationIndex(string familyName,
            Region region,
            bool revised,
            bool interpolated,
            bool ratio, // is this one a genuine index or a ratio?
            Frequency frequency,
            Period availabilityLag,
            Currency currency,
            Handle<YoYInflationTermStructure> yoyInflation = null)
            : base(familyName, region, revised, interpolated, frequency, availabilityLag, currency)
        {
            ratio_ = ratio;
            yoyInflation_ = yoyInflation ?? new Handle<YoYInflationTermStructure>();
            yoyInflation_.registerWith(update);
        }

        public YoYInflationIndex clone(Handle<YoYInflationTermStructure> h) =>
            new YoYInflationIndex(familyName_, region_, revised_,
                interpolated_, ratio_, frequency_,
                availabilityLag_, currency_, h);

        // Index interface
        // The forecastTodaysFixing parameter (required by the Index interface) is currently ignored.
        public override double fixing(Date fixingDate, bool forecastTodaysFixing = false)
        {
            var today = Settings.evaluationDate();
            var todayMinusLag = today - availabilityLag_;
            var limm = Termstructures.Utils.inflationPeriod(todayMinusLag, frequency_);
            var lastFix = limm.Key - 1;

            var flatMustForecastOn = lastFix + 1;
            var interpMustForecastOn = lastFix + 1 - new Period(frequency_);

            if (interpolated() && fixingDate >= interpMustForecastOn)
            {
                return forecastFixing(fixingDate);
            }

            if (!interpolated() && fixingDate >= flatMustForecastOn)
            {
                return forecastFixing(fixingDate);
            }

            // four cases with ratio() and interpolated()
            if (ratio())
            {
                if (interpolated())
                {
                    // IS ratio, IS interpolated
                    var lim = Termstructures.Utils.inflationPeriod(fixingDate, frequency_);
                    var fixMinus1Y = new NullCalendar().advance(fixingDate, new Period(-1, TimeUnit.Years), BusinessDayConvention.ModifiedFollowing);
                    var limBef = Termstructures.Utils.inflationPeriod(fixMinus1Y, frequency_);
                    double dp = lim.Value + 1 - lim.Key;
                    double dpBef = limBef.Value + 1 - limBef.Key;
                    double dl = fixingDate - lim.Key;
                    // potentially does not work on 29th Feb
                    double dlBef = fixMinus1Y - limBef.Key;
                    // get the four relevant fixings
                    // recall that they are stored flat for every day
                    var limFirstFix =
                        IndexManager.instance().getHistory(name())[lim.Key];
                    QLNet.Utils.QL_REQUIRE(limFirstFix != null, () => "Missing " + name() + " fixing for " + lim.Key);
                    var limSecondFix =
                        IndexManager.instance().getHistory(name())[lim.Value + 1];
                    QLNet.Utils.QL_REQUIRE(limSecondFix != null, () => "Missing " + name() + " fixing for " + lim.Value + 1);
                    var limBefFirstFix =
                        IndexManager.instance().getHistory(name())[limBef.Key];
                    QLNet.Utils.QL_REQUIRE(limBefFirstFix != null, () => "Missing " + name() + " fixing for " + limBef.Key);
                    var limBefSecondFix =
                        IndexManager.instance().getHistory(name())[limBef.Value + 1];
                    QLNet.Utils.QL_REQUIRE(limBefSecondFix != null, () => "Missing " + name() + " fixing for " + limBef.Value + 1);

                    var linearNow = limFirstFix.Value + (limSecondFix.Value - limFirstFix.Value) * dl / dp;
                    var linearBef = limBefFirstFix.Value + (limBefSecondFix.Value - limBefFirstFix.Value) * dlBef / dpBef;
                    var wasYES = linearNow / linearBef - 1.0;

                    return wasYES;
                }

                // IS ratio, NOT interpolated
                var pastFixing = IndexManager.instance().getHistory(name())[fixingDate];
                QLNet.Utils.QL_REQUIRE(pastFixing != null, () => "Missing " + name() + " fixing for " + fixingDate);
                var previousDate = fixingDate - new Period(1, TimeUnit.Years);
                var previousFixing = IndexManager.instance().getHistory(name())[previousDate];
                QLNet.Utils.QL_REQUIRE(previousFixing != null, () => "Missing " + name() + " fixing for " + previousDate);
                return pastFixing.Value / previousFixing.Value - 1.0;
            }

            // NOT ratio
            if (interpolated())
            {
                // NOT ratio, IS interpolated
                var lim = Termstructures.Utils.inflationPeriod(fixingDate, frequency_);
                double dp = lim.Value + 1 - lim.Key;
                double dl = fixingDate - lim.Key;
                var limFirstFix = IndexManager.instance().getHistory(name())[lim.Key];
                QLNet.Utils.QL_REQUIRE(limFirstFix != null, () => "Missing " + name() + " fixing for " + lim.Key);
                var limSecondFix = IndexManager.instance().getHistory(name())[lim.Value + 1];
                QLNet.Utils.QL_REQUIRE(limSecondFix != null, () => "Missing " + name() + " fixing for " + lim.Value + 1);
                var linearNow = limFirstFix.Value + (limSecondFix.Value - limFirstFix.Value) * dl / dp;
                return linearNow;
            }

            {
                // NOT ratio, NOT interpolated
                // so just flat
                var pastFixing = IndexManager.instance().getHistory(name())[fixingDate];
                QLNet.Utils.QL_REQUIRE(pastFixing != null, () => "Missing " + name() + " fixing for " + fixingDate);
                return pastFixing.Value;
            }
        }

        // Other methods
        public bool ratio() => ratio_;

        public Handle<YoYInflationTermStructure> yoyInflationTermStructure() => yoyInflation_;

        private double forecastFixing(Date fixingDate)
        {
            Date d;
            if (interpolated())
            {
                d = fixingDate;
            }
            else
            {
                // if the value is not interpolated use the starting value
                // by internal convention this will be consistent
                var lim = Termstructures.Utils.inflationPeriod(fixingDate, frequency_);
                d = lim.Key;
            }

            return yoyInflation_.link.yoyRate(d, new Period(0, TimeUnit.Days));
        }
    }
}
