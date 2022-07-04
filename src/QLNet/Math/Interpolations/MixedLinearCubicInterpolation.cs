using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class MixedLinearCubicInterpolation : Interpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public MixedLinearCubicInterpolation(List<double> xBegin, int xEnd,
            List<double> yBegin, int n,
            Behavior behavior,
            CubicInterpolation.DerivativeApprox da,
            bool monotonic,
            CubicInterpolation.BoundaryCondition leftC,
            double leftConditionValue,
            CubicInterpolation.BoundaryCondition rightC,
            double rightConditionValue)
        {
            impl_ = new MixedInterpolationImpl<Linear, Cubic>(xBegin, xEnd, yBegin, n, behavior,
                new Linear(),
                new Cubic(da, monotonic, leftC, leftConditionValue, rightC, rightConditionValue));
            impl_.update();
        }
    }
}