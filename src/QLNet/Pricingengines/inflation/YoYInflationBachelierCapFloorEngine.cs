using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Pricingengines.inflation
{
    [PublicAPI]
    public class YoYInflationBachelierCapFloorEngine : YoYInflationCapFloorEngine
    {
        public YoYInflationBachelierCapFloorEngine(YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> vol)
            : base(index, vol)
        {
        }

        protected override double optionletImpl(QLNet.Option.Type type, double strike,
            double forward, double stdDev,
            double d) =>
            Utils.bachelierBlackFormula(type, strike, forward, stdDev, d);
    }
}
