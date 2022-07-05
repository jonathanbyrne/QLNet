using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.PricingEngines.inflation
{
    [PublicAPI]
    public class YoYInflationBlackCapFloorEngine : YoYInflationCapFloorEngine
    {
        public YoYInflationBlackCapFloorEngine(YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> volatility)
            : base(index, volatility)
        {
        }

        protected override double optionletImpl(QLNet.Option.Type type, double strike, double forward, double stdDev,
            double d) =>
            Utils.blackFormula(type, strike, forward, stdDev, d);
    }
}
