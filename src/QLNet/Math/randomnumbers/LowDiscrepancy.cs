using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.RandomNumbers
{
    [PublicAPI]
    public class LowDiscrepancy : GenericLowDiscrepancy<SobolRsg, InverseCumulativeNormal>
    {
    }
}
