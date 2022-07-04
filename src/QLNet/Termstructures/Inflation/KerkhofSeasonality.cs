using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class KerkhofSeasonality : MultiplicativePriceSeasonality
    {
        public KerkhofSeasonality(Date seasonalityBaseDate, List<double> seasonalityFactors)
            : base(seasonalityBaseDate, Frequency.Monthly, seasonalityFactors)
        { }

        public override double seasonalityFactor(Date to)
        {
            var dir = 1;
            var from = seasonalityBaseDate();
            var fromMonth = from.month();
            var toMonth = to.month();

            var factorPeriod = new Period(frequency());

            if (toMonth < fromMonth)
            {
                var dummy = fromMonth;
                fromMonth = toMonth;
                toMonth = dummy;
                dir = 0; // We calculate invers Factor in loop
            }

            Utils.QL_REQUIRE(seasonalityFactors().Count == 12 &&
                             factorPeriod.units() == TimeUnit.Months, () =>
                "12 monthly seasonal factors needed for Kerkhof Seasonality:" +
                " got " + seasonalityFactors().Count);

            var seasonalCorrection = 1.0;
            for (var i = fromMonth; i < toMonth; i++)
            {
                seasonalCorrection *= seasonalityFactors()[i];
            }

            if (dir == 0) // invers Factor required
            {
                seasonalCorrection = 1 / seasonalCorrection;
            }

            return seasonalCorrection;

        }

        protected override double seasonalityCorrection(double rate, Date atDate, DayCounter dc, Date curveBaseDate, bool isZeroRate)
        {
            var indexFactor = seasonalityFactor(atDate);

            // Getting seasonality correction
            double f = 0;
            if (isZeroRate)
            {
                var lim = Utils.inflationPeriod(curveBaseDate, Frequency.Monthly);
                var timeFromCurveBase = dc.yearFraction(lim.Key, atDate);
                f = System.Math.Pow(indexFactor, 1 / timeFromCurveBase);
            }
            else
            {
                Utils.QL_FAIL("Seasonal Kerkhof model is not defined on YoY rates");
            }

            return (rate + 1) * f - 1;
        }
    }
}