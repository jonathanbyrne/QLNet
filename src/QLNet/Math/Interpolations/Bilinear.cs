using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class Bilinear : IInterpolationFactory2D
    {
        public Interpolation2D interpolate(List<double> xBegin, int xSize,
            List<double> yBegin, int ySize,
            Matrix zData) =>
            new BilinearInterpolation(xBegin, xSize, yBegin, ySize, zData);
    }
}