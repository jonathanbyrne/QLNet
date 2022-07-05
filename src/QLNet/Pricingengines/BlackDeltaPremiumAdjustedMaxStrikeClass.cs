using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Quotes;

namespace QLNet.PricingEngines
{
    [PublicAPI]
    public class BlackDeltaPremiumAdjustedMaxStrikeClass : ISolver1d
    {
        private BlackDeltaCalculator bdc_;
        private double stdDev_;

        public BlackDeltaPremiumAdjustedMaxStrikeClass(QLNet.Option.Type ot,
            DeltaVolQuote.DeltaType dt,
            double spot,
            double dDiscount, // domestic discount
            double fDiscount, // foreign  discount
            double stdDev)
        {
            bdc_ = new BlackDeltaCalculator(ot, dt, spot, dDiscount, fDiscount, stdDev);
            stdDev_ = stdDev;
        }

        public override double value(double strike) => bdc_.cumD2(strike) * stdDev_ - bdc_.nD2(strike);
    }
}
