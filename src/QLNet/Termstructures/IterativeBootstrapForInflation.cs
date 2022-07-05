using JetBrains.Annotations;
using QLNet.Termstructures.Inflation;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public class IterativeBootstrapForInflation : IterativeBootstrap<PiecewiseZeroInflationCurve, ZeroInflationTermStructure>
    {
    }
}
