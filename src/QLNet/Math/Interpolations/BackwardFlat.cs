using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class BackwardFlat : IInterpolationFactory
    {
        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new BackwardFlatInterpolation(xBegin, size, yBegin);

        public bool global => false;

        public int requiredPoints => 2;
    }
}