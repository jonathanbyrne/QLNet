using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public interface IInterpolationFactory2D
    {
        Interpolation2D interpolate(List<double> xBegin, int xSize,
            List<double> yBegin, int ySize,
            Matrix zData);
    }
}