using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ForwardFlat : IInterpolationFactory
    {
        public bool global => false;

        public int requiredPoints => 2;

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new ForwardFlatInterpolation(xBegin, size, yBegin);
    }
}
