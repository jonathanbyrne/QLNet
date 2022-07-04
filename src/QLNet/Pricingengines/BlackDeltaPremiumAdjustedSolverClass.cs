using QLNet.Math;
using QLNet.Quotes;

namespace QLNet.Pricingengines
{
    [JetBrains.Annotations.PublicAPI] public class BlackDeltaPremiumAdjustedSolverClass : ISolver1d
    {
        public BlackDeltaPremiumAdjustedSolverClass(QLNet.Option.Type ot,
            DeltaVolQuote.DeltaType dt,
            double spot,
            double dDiscount,   // domestic discount
            double fDiscount,   // foreign  discount
            double stdDev,
            double delta)
        {
            bdc_ = new BlackDeltaCalculator(ot, dt, spot, dDiscount, fDiscount, stdDev);
            delta_ = delta;
        }

        public override double value(double strike) => bdc_.deltaFromStrike(strike) - delta_;

        private BlackDeltaCalculator bdc_;
        private double delta_;
    }
}