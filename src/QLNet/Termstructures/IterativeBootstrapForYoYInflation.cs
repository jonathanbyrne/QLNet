using JetBrains.Annotations;
using QLNet.Termstructures.Inflation;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public class IterativeBootstrapForYoYInflation : IterativeBootstrap<PiecewiseYoYInflationCurve, YoYInflationTermStructure>
    {
    }
}
