using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class MixedLinearCubicNaturalSpline : MixedLinearCubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public MixedLinearCubicNaturalSpline(List<double> xBegin, int xEnd, List<double> yBegin, int n,
            Behavior behavior = Behavior.ShareRanges)
            : base(xBegin, xEnd, yBegin, n, behavior,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }
}
