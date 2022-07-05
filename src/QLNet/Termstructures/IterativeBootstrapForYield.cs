using JetBrains.Annotations;
using QLNet.Termstructures.Yield;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public class IterativeBootstrapForYield : IterativeBootstrap<PiecewiseYieldCurve, YieldTermStructure>
    {
    }
}
