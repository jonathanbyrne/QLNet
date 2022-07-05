using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class ZeroInflationIndex : InflationIndex
    {
        private Handle<ZeroInflationTermStructure> zeroInflation_;

        //! Always use the evaluation date as the reference date
        public ZeroInflationIndex(string familyName,
            Region region,
            bool revised,
            bool interpolated,
            Frequency frequency,
            Period availabilityLag,
            Currency currency,
            Handle<ZeroInflationTermStructure> ts = null)
            : base(familyName, region, revised, interpolated,
                frequency, availabilityLag, currency)
        {
            zeroInflation_ = ts ?? new Handle<ZeroInflationTermStructure>();
            zeroInflation_.registerWith(update);
        }

        public ZeroInflationIndex clone(Handle<ZeroInflationTermStructure> h) =>
            new ZeroInflationIndex(familyName_, region_, revised_,
                interpolated_, frequency_,
                availabilityLag_, currency_, h);

        /*! \warning the forecastTodaysFixing parameter (required by
                     the Index interface) is currently ignored.
        */
        public override double fixing(Date aFixingDate, bool forecastTodaysFixing = false)
        {
            if (!needsForecast(aFixingDate))
            {
                var lim = Utils.inflationPeriod(aFixingDate, frequency_);
                Utils.QL_REQUIRE(IndexManager.instance().getHistory(name()).ContainsKey(lim.Key), () =>
                    "Missing " + name() + " fixing for " + lim.Key);

                var pastFixing = IndexManager.instance().getHistory(name())[lim.Key];
                var theFixing = pastFixing;
                if (interpolated_)
                {
                    // fixings stored on first day of every period
                    if (aFixingDate == lim.Key)
                    {
                        // we don't actually need the next fixing
                        theFixing = pastFixing;
                    }
                    else
                    {
                        Utils.QL_REQUIRE(IndexManager.instance().getHistory(name()).ContainsKey(lim.Value + 1), () =>
                            "Missing " + name() + " fixing for " + (lim.Value + 1));

                        var pastFixing2 = IndexManager.instance().getHistory(name())[lim.Value + 1];

                        // Use lagged period for interpolation
                        var reference_period_lim = Utils.inflationPeriod(aFixingDate + zeroInflationTermStructure().link.observationLag(), frequency_);

                        // now linearly interpolate
                        double daysInPeriod = reference_period_lim.Value + 1 - reference_period_lim.Key;
                        theFixing = pastFixing + (pastFixing2 - pastFixing) * (aFixingDate - lim.Key) / daysInPeriod;
                    }
                }

                return theFixing.GetValueOrDefault();
            }

            return forecastFixing(aFixingDate);
        }

        // Other methods
        public Handle<ZeroInflationTermStructure> zeroInflationTermStructure() => zeroInflation_;

        private double forecastFixing(Date fixingDate)
        {
            // the term structure is relative to the fixing value at the base date.
            var baseDate = zeroInflation_.link.baseDate();
            Utils.QL_REQUIRE(!needsForecast(baseDate), () => name() + " index fixing at base date is not available");
            var baseFixing = fixing(baseDate);
            Date effectiveFixingDate;
            if (interpolated())
            {
                effectiveFixingDate = fixingDate;
            }
            else
            {
                // start of period is the convention
                // so it's easier to do linear interpolation on fixings
                effectiveFixingDate = Utils.inflationPeriod(fixingDate, frequency()).Key;
            }

            // no observation lag because it is the fixing for the date
            // but if index is not interpolated then that fixing is constant
            // for each period, hence the t uses the effectiveFixingDate
            // However, it's slightly safe to get the zeroRate with the
            // fixingDate to avoid potential problems at the edges of periods
            var t = zeroInflation_.link.dayCounter().yearFraction(baseDate, effectiveFixingDate);
            var forceLinearInterpolation = false;
            var zero = zeroInflation_.link.zeroRate(fixingDate, new Period(0, TimeUnit.Days), forceLinearInterpolation);
            // Annual compounding is the convention for zero inflation rates (or quotes)
            return baseFixing * System.Math.Pow(1.0 + zero, t);
        }

        private bool needsForecast(Date fixingDate)
        {
            // Stored fixings are always non-interpolated.
            // If an interpolated fixing is required then
            // the availability lag + one inflation period
            // must have passed to use historical fixings
            // (because you need the next one to interpolate).
            // The interpolation is calculated (linearly) on demand.

            var today = Settings.evaluationDate();
            var todayMinusLag = today - availabilityLag_;

            var historicalFixingKnown = Utils.inflationPeriod(todayMinusLag, frequency_).Key - 1;
            var latestNeededDate = fixingDate;

            if (interpolated_)
            {
                // might need the next one too
                var p = Utils.inflationPeriod(fixingDate, frequency_);
                if (fixingDate > p.Key)
                {
                    latestNeededDate = latestNeededDate + new Period(frequency_);
                }
            }

            if (latestNeededDate <= historicalFixingKnown)
            {
                // the fixing date is well before the availability lag, so
                // we know that fixings were provided.
                return false;
            }

            if (latestNeededDate > today)
            {
                // the fixing can't be available, no matter what's in the
                // time series
                return true;
            }

            // we're not sure, but the fixing might be there so we
            // check.  Todo: check which fixings are not possible, to
            // avoid using fixings in the future
            return !timeSeries().ContainsKey(latestNeededDate);
        }
    }
}
