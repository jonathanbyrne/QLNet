using QLNet.Math;
using QLNet.Quotes;

namespace QLNet.Pricingengines
{
    [JetBrains.Annotations.PublicAPI] public class BlackDeltaPremiumAdjustedMaxStrikeClass : ISolver1d
    {
        public BlackDeltaPremiumAdjustedMaxStrikeClass(QLNet.Option.Type ot,
            DeltaVolQuote.DeltaType dt,
            double spot,
            double dDiscount,   // domestic discount
            double fDiscount,   // foreign  discount
            double stdDev)
        {
            bdc_ = new BlackDeltaCalculator(ot, dt, spot, dDiscount, fDiscount, stdDev);
            stdDev_ = stdDev;
        }

        public override double value(double strike) => bdc_.cumD2(strike) * stdDev_ - bdc_.nD2(strike);

        private BlackDeltaCalculator bdc_;
        private double stdDev_;
    }
}