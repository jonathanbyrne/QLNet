using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math
{
    [PublicAPI]
    public interface IInterpolationFactory
    {
        bool global { get; }

        int requiredPoints { get; }

        Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin);
    }
}
