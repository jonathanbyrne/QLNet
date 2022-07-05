using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class CubicNaturalSpline : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public CubicNaturalSpline(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.Spline, false,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }
}
