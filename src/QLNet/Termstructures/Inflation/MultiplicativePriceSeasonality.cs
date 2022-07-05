using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [PublicAPI]
    public class MultiplicativePriceSeasonality : Seasonality
    {
        private Frequency frequency_;
        private Date seasonalityBaseDate_;
        private List<double> seasonalityFactors_;

        //Constructors
        //
        public MultiplicativePriceSeasonality()
        {
        }

        public MultiplicativePriceSeasonality(Date seasonalityBaseDate, Frequency frequency,
            List<double> seasonalityFactors)
        {
            set(seasonalityBaseDate, frequency, seasonalityFactors);
        }

        public override double correctYoYRate(Date d, double r, InflationTermStructure iTS)
        {
            var lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
            var curveBaseDate = lim.Value;
            return seasonalityCorrection(r, d, iTS.dayCounter(), curveBaseDate, false);
        }

        // Seasonality interface
        public override double correctZeroRate(Date d, double r, InflationTermStructure iTS)
        {
            var lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
            var curveBaseDate = lim.Value;
            return seasonalityCorrection(r, d, iTS.dayCounter(), curveBaseDate, true);
        }

        public virtual Frequency frequency() => frequency_;

        public override bool isConsistent(InflationTermStructure iTS)
        {
            // If multi-year is the specification consistent with the term structure start date?
            // We do NOT test daily seasonality because this will, in general, never be consistent
            // given weekends, holidays, leap years, etc.
            if (frequency() == Frequency.Daily)
            {
                return true;
            }

            if ((int)frequency() == seasonalityFactors().Count)
            {
                return true;
            }

            // how many years do you need to test?
            var nTest = seasonalityFactors().Count / (int)frequency();
            // ... relative to the start of the inflation curve
            var lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
            var curveBaseDate = lim.Value;
            var factorBase = seasonalityFactor(curveBaseDate);

            var eps = 0.00001;
            for (var i = 1; i < nTest; i++)
            {
                var factorAt = seasonalityFactor(curveBaseDate + new Period(i, TimeUnit.Years));
                Utils.QL_REQUIRE(System.Math.Abs(factorAt - factorBase) < eps, () =>
                    "seasonality is inconsistent with inflation " +
                    "term structure, factors " + factorBase + " and later factor "
                    + factorAt + ", " + i + " years later from inflation curve "
                    + " with base date at " + curveBaseDate);
            }

            return true;
        }

        //! inspectors
        public virtual Date seasonalityBaseDate() => seasonalityBaseDate_;

        //! The factor returned is NOT normalized relative to ANYTHING.
        public virtual double seasonalityFactor(Date to)
        {
            var from = seasonalityBaseDate();
            var factorFrequency = frequency();
            var nFactors = seasonalityFactors().Count;
            var factorPeriod = new Period(factorFrequency);
            var which = 0;
            if (from == to)
            {
                which = 0;
            }
            else
            {
                // days, weeks, months, years are the only time unit possibilities
                var diffDays = System.Math.Abs(to - from); // in days
                var dir = 1;
                if (from > to)
                {
                    dir = -1;
                }

                var diff = 0;
                if (factorPeriod.units() == TimeUnit.Days)
                {
                    diff = dir * diffDays;
                }
                else if (factorPeriod.units() == TimeUnit.Weeks)
                {
                    diff = dir * (diffDays / 7);
                }
                else if (factorPeriod.units() == TimeUnit.Months)
                {
                    var lim = Utils.inflationPeriod(to, factorFrequency);
                    diff = diffDays / (31 * factorPeriod.length());
                    var go = from + dir * diff * factorPeriod;
                    while (!(lim.Key <= go && go <= lim.Value))
                    {
                        go += dir * factorPeriod;
                        diff++;
                    }

                    diff = dir * diff;
                }
                else if (factorPeriod.units() == TimeUnit.Years)
                {
                    Utils.QL_FAIL("seasonality period time unit is not allowed to be : " + factorPeriod.units());
                }
                else
                {
                    Utils.QL_FAIL("Unknown time unit: " + factorPeriod.units());
                }
                // now adjust to the available number of factors, direction dependent

                if (dir == 1)
                {
                    which = diff % nFactors;
                }
                else
                {
                    which = (nFactors - (-diff % nFactors)) % nFactors;
                }
            }

            return seasonalityFactors()[which];
        }

        public virtual List<double> seasonalityFactors() => seasonalityFactors_;

        public virtual void set(Date seasonalityBaseDate, Frequency frequency,
            List<double> seasonalityFactors)
        {
            frequency_ = frequency;
            seasonalityFactors_ = new List<double>(seasonalityFactors.Count);

            for (var i = 0; i < seasonalityFactors.Count; i++)
            {
                seasonalityFactors_.Add(seasonalityFactors[i]);
            }

            seasonalityBaseDate_ = seasonalityBaseDate;
            validate();
        }

        protected virtual double seasonalityCorrection(double rate, Date atDate, DayCounter dc,
            Date curveBaseDate, bool isZeroRate)
        {
            // need _two_ corrections in order to get: seasonality = factor[atDate-seasonalityBase] / factor[reference-seasonalityBase]
            // i.e. for ZERO inflation rates you have the true fixing at the curve base so this factor must be normalized to one
            //      for YoY inflation rates your reference point is the year before

            var factorAt = seasonalityFactor(atDate);

            //Getting seasonality correction for either ZC or YoY
            double f;
            if (isZeroRate)
            {
                var factorBase = seasonalityFactor(curveBaseDate);
                var seasonalityAt = factorAt / factorBase;
                var timeFromCurveBase = dc.yearFraction(curveBaseDate, atDate);
                f = System.Math.Pow(seasonalityAt, 1 / timeFromCurveBase);
            }
            else
            {
                var factor1Ybefore = seasonalityFactor(atDate - new Period(1, TimeUnit.Years));
                f = factorAt / factor1Ybefore;
            }

            return (rate + 1) * f - 1;
        }

        protected virtual void validate()
        {
            switch (frequency())
            {
                case Frequency.Semiannual: //2
                case Frequency.EveryFourthMonth: //3
                case Frequency.Quarterly: //4
                case Frequency.Bimonthly: //6
                case Frequency.Monthly: //12
                case Frequency.Biweekly: // etc.
                case Frequency.Weekly:
                case Frequency.Daily:
                    Utils.QL_REQUIRE(seasonalityFactors().Count % (int)frequency() == 0, () =>
                        "For frequency " + frequency()
                                         + " require multiple of " + (int)frequency() + " factors "
                                         + seasonalityFactors().Count + " were given.");
                    break;
                default:
                    Utils.QL_FAIL("bad frequency specified: " + frequency() + ", only semi-annual through daily permitted.");
                    break;
            }
        }
    }
}
