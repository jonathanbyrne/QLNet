using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class Bilinear : IInterpolationFactory2D
    {
        public Interpolation2D interpolate(List<double> xBegin, int xSize,
            List<double> yBegin, int ySize,
            Matrix zData) =>
            new BilinearInterpolation(xBegin, xSize, yBegin, ySize, zData);
    }
}
