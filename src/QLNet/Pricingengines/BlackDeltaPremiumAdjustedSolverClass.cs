using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Quotes;

namespace QLNet.PricingEngines
{
    [PublicAPI]
    public class BlackDeltaPremiumAdjustedSolverClass : ISolver1d
    {
        private BlackDeltaCalculator bdc_;
        private double delta_;

        public BlackDeltaPremiumAdjustedSolverClass(QLNet.Option.Type ot,
            DeltaVolQuote.DeltaType dt,
            double spot,
            double dDiscount, // domestic discount
            double fDiscount, // foreign  discount
            double stdDev,
            double delta)
        {
            bdc_ = new BlackDeltaCalculator(ot, dt, spot, dDiscount, fDiscount, stdDev);
            delta_ = delta;
        }

        public override double value(double strike) => bdc_.deltaFromStrike(strike) - delta_;
    }
}
