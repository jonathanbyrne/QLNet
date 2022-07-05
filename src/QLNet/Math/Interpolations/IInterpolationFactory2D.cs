using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public interface IInterpolationFactory2D
    {
        Interpolation2D interpolate(List<double> xBegin, int xSize,
            List<double> yBegin, int ySize,
            Matrix zData);
    }
}
