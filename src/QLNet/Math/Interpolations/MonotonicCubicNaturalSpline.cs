using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class MonotonicCubicNaturalSpline : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public MonotonicCubicNaturalSpline(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.Spline, true,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        { }
    }
}