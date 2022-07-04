using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class LogCubicInterpolation : Interpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */

        public LogCubicInterpolation(List<double> xBegin, int size, List<double> yBegin,
            CubicInterpolation.DerivativeApprox da,
            bool monotonic,
            CubicInterpolation.BoundaryCondition leftC,
            double leftConditionValue,
            CubicInterpolation.BoundaryCondition rightC,
            double rightConditionValue)
        {
            impl_ = new LogInterpolationImpl<Cubic>(xBegin, size, yBegin,
                new Cubic(da, monotonic, leftC, leftConditionValue, rightC, rightConditionValue));
            impl_.update();
        }
    }
}