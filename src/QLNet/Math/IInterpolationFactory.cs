using System.Collections.Generic;

namespace QLNet.Math
{
    [JetBrains.Annotations.PublicAPI] public interface IInterpolationFactory
    {
        Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin);
        bool global { get; }
        int requiredPoints { get; }
    }
}