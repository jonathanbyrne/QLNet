using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class Parabolic : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public Parabolic(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.Parabolic, false,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        { }
    }
}