using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.PricingEngines.inflation
{
    [PublicAPI]
    public class YoYInflationUnitDisplacedBlackCapFloorEngine : YoYInflationCapFloorEngine
    {
        public YoYInflationUnitDisplacedBlackCapFloorEngine(YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> vol)
            : base(index, vol)
        {
        }

        protected override double optionletImpl(QLNet.Option.Type type, double strike,
            double forward, double stdDev,
            double d) =>
            // could use displacement parameter in blackFormula but this is clearer
            Utils.blackFormula(type, strike + 1.0, forward + 1.0, stdDev, d);
    }
}
