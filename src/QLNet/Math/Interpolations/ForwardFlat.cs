using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class ForwardFlat : IInterpolationFactory
    {
        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new ForwardFlatInterpolation(xBegin, size, yBegin);

        public bool global => false;

        public int requiredPoints => 2;
    }
}