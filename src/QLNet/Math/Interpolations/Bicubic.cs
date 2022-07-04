using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class Bicubic : IInterpolationFactory2D
    {
        public Interpolation2D interpolate(List<double> xBegin, int size, List<double> yBegin, int ySize, Matrix zData) => new BicubicSpline(xBegin, size, yBegin, ySize, zData);
    }
}