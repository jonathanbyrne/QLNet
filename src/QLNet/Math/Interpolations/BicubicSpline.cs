using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class BicubicSpline : Interpolation2D
    {
        /*! \pre the \f$ x \f$ and \f$ y \f$ values must be sorted. */
        public BicubicSpline(List<double> xBegin, int size, List<double> yBegin, int ySize, Matrix zData)
        {
            impl_ = new BicubicSplineImpl(xBegin, size, yBegin, ySize, zData);
        }

        public double derivativeX(double x, double y) => ((IBicubicSplineDerivatives)impl_).derivativeX(x, y);

        public double derivativeXY(double x, double y) => ((IBicubicSplineDerivatives)impl_).derivativeXY(x, y);

        public double derivativeY(double x, double y) => ((IBicubicSplineDerivatives)impl_).derivativeY(x, y);

        public double secondDerivativeX(double x, double y) => ((IBicubicSplineDerivatives)impl_).secondDerivativeX(x, y);

        public double secondDerivativeY(double x, double y) => ((IBicubicSplineDerivatives)impl_).secondDerivativeY(x, y);
    }
}
