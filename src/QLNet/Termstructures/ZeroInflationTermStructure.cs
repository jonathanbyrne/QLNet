using QLNet.Termstructures;
using QLNet.Termstructures.Inflation;
using QLNet.Time;

namespace QLNet
{
    public abstract class ZeroInflationTermStructure : InflationTermStructure
    {
        protected ZeroInflationTermStructure()
        {}

        // Constructors
        protected ZeroInflationTermStructure(DayCounter dayCounter,
            double baseZeroRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(baseZeroRate, observationLag, frequency, indexIsInterpolated,
                yTS, dayCounter, seasonality)
        {}

        protected ZeroInflationTermStructure(Date referenceDate,
            Calendar calendar,
            DayCounter dayCounter,
            double baseZeroRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(referenceDate, baseZeroRate, observationLag, frequency, indexIsInterpolated,
                yTS, calendar, dayCounter, seasonality)
        {}

        protected ZeroInflationTermStructure(int settlementDays,
            Calendar calendar,
            DayCounter dayCounter,
            double baseZeroRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Seasonality seasonality = null)
            : base(settlementDays, calendar, baseZeroRate, observationLag, frequency,
                indexIsInterpolated, yTS, dayCounter, seasonality)
        {}

        // Inspectors
        //! zero-coupon inflation rate for an instrument with maturity (pay date) d
        //! that observes with given lag and interpolation.
        //! Since inflation is highly linked to dates (lags, interpolation, months for seasonality, etc)
        //! we do NOT provide a "time" version of the rate lookup.
        /*! Essentially the fair rate for a zero-coupon inflation swap
          (by definition), i.e. the zero term structure uses yearly
          compounding, which is assumed for ZCIIS instrument quotes.
          N.B. by default you get the same as lag and interpolation
          as the term structure.
          If you want to get predictions of RPI/CPI/etc then use an
          index.
      */

        public double zeroRate(Date d) => zeroRate(d, new Period(-1, TimeUnit.Days), false, false);

        public double zeroRate(Date d, Period instObsLag) => zeroRate(d, instObsLag, false, false);

        public double zeroRate(Date d, Period instObsLag, bool forceLinearInterpolation) => zeroRate(d, instObsLag, forceLinearInterpolation, false);

        public double zeroRate(Date d, Period instObsLag,
            bool forceLinearInterpolation,
            bool extrapolate)
        {
            var useLag = instObsLag;
            if (instObsLag == new Period(-1, TimeUnit.Days))
            {
                useLag = observationLag();
            }

            double zeroRate;
            if (forceLinearInterpolation)
            {
                var dd = Utils.inflationPeriod(d - useLag, frequency());
                var ddValue = dd.Value + new Period(1, TimeUnit.Days);
                double dp = ddValue - dd.Key;
                double dt = d - dd.Key;
                // if we are interpolating we only check the exact point
                // this prevents falling off the end at curve maturity
                base.checkRange(d, extrapolate);
                var t1 = timeFromReference(dd.Key);
                var t2 = timeFromReference(ddValue);
                zeroRate = zeroRateImpl(t1) + zeroRateImpl(t2) * (dt / dp);
            }
            else
            {
                if (indexIsInterpolated())
                {
                    base.checkRange(d - useLag, extrapolate);
                    var t = timeFromReference(d - useLag);
                    zeroRate = zeroRateImpl(t);
                }
                else
                {
                    var dd = Utils.inflationPeriod(d - useLag, frequency());
                    base.checkRange(dd.Key, extrapolate);
                    var t = timeFromReference(dd.Key);
                    zeroRate = zeroRateImpl(t);
                }
            }

            if (hasSeasonality())
            {
                zeroRate = seasonality().correctZeroRate(d - useLag, zeroRate, this);
            }


            return zeroRate;
        }

        //! to be defined in derived classes
        protected abstract double zeroRateImpl(double t);
    }
}