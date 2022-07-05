using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class CubicSplineOvershootingMinimization2 : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public CubicSplineOvershootingMinimization2(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.SplineOM2, false,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }
}
