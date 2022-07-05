using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class CubicSplineOvershootingMinimization1 : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public CubicSplineOvershootingMinimization1(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.SplineOM1, false,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }
}
