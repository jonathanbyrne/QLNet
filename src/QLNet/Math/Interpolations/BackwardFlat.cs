using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class BackwardFlat : IInterpolationFactory
    {
        public bool global => false;

        public int requiredPoints => 2;

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new BackwardFlatInterpolation(xBegin, size, yBegin);
    }
}
