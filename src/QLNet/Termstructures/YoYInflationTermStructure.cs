using QLNet.Termstructures.Inflation;
using QLNet.Time;

namespace QLNet.Termstructures
{
    public abstract class YoYInflationTermStructure : InflationTermStructure
    {
        protected YoYInflationTermStructure()
        {
        }

        // Constructors
        protected YoYInflationTermStructure(DayCounter dayCounter,
            double baseYoYRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(baseYoYRate, observationLag, frequency, indexIsInterpolated,
                yTS, dayCounter, seasonality)
        {
        }

        protected YoYInflationTermStructure(Date referenceDate,
            Calendar calendar,
            DayCounter dayCounter,
            double baseYoYRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(referenceDate, baseYoYRate, observationLag, frequency, indexIsInterpolated,
                yTS, calendar, dayCounter, seasonality)
        {
        }

        protected YoYInflationTermStructure(int settlementDays,
            Calendar calendar,
            DayCounter dayCounter,
            double baseYoYRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(settlementDays, calendar, baseYoYRate, observationLag,
                frequency, indexIsInterpolated,
                yTS, dayCounter, seasonality)
        {
        }

        // Inspectors
        //! year-on-year inflation rate, forceLinearInterpolation
        //! is relative to the frequency of the TS.
        //! Since inflation is highly linked to dates (lags, interpolation, months for seasonality etc)
        //! we do NOT provide a "time" version of the rate lookup.
        /*! \note this is not the year-on-year swap (YYIIS) rate. */

        public double yoyRate(Date d) => yoyRate(d, new Period(-1, TimeUnit.Days), false, false);

        public double yoyRate(Date d, Period instObsLag) => yoyRate(d, instObsLag, false, false);

        public double yoyRate(Date d, Period instObsLag, bool forceLinearInterpolation) => yoyRate(d, instObsLag, forceLinearInterpolation, false);

        public double yoyRate(Date d, Period instObsLag, bool forceLinearInterpolation,
            bool extrapolate)
        {
            var useLag = instObsLag;
            if (instObsLag == new Period(-1, TimeUnit.Days))
            {
                useLag = observationLag();
            }

            double yoyRate;
            if (forceLinearInterpolation)
            {
                var dd = Utils.inflationPeriod(d - useLag, frequency());
                var ddValue = dd.Value + new Period(1, TimeUnit.Days);
                double dp = ddValue - dd.Key;
                double dt = (d - useLag) - dd.Key;
                // if we are interpolating we only check the exact point
                // this prevents falling off the end at curve maturity
                base.checkRange(d, extrapolate);
                var t1 = timeFromReference(dd.Key);
                var t2 = timeFromReference(dd.Value);
                yoyRate = yoyRateImpl(t1) + (yoyRateImpl(t2) - yoyRateImpl(t1)) * (dt / dp);
            }
            else
            {
                if (indexIsInterpolated())
                {
                    base.checkRange(d - useLag, extrapolate);
                    var t = timeFromReference(d - useLag);
                    yoyRate = yoyRateImpl(t);
                }
                else
                {
                    var dd = Utils.inflationPeriod(d - useLag, frequency());
                    base.checkRange(dd.Key, extrapolate);
                    var t = timeFromReference(dd.Key);
                    yoyRate = yoyRateImpl(t);
                }
            }

            if (hasSeasonality())
            {
                yoyRate = seasonality().correctYoYRate(d - useLag, yoyRate, this);
            }

            return yoyRate;
        }

        //! to be defined in derived classes
        protected abstract double yoyRateImpl(double time);
    }
}
