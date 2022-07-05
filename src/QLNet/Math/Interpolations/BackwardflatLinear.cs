using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class BackwardflatLinear
    {
        public Interpolation2D interpolate(List<double> xBegin, int xEnd, List<double> yBegin, int yEnd, Matrix z) => new BackwardflatLinearInterpolation(xBegin, xEnd, yBegin, yEnd, z);
    }
}
