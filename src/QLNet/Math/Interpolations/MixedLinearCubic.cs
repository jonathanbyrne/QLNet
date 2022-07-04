using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class MixedLinearCubic
    {
        public MixedLinearCubic(int n,
            Behavior behavior,
            CubicInterpolation.DerivativeApprox da,
            bool monotonic = true,
            CubicInterpolation.BoundaryCondition leftCondition = CubicInterpolation.BoundaryCondition.SecondDerivative,
            double leftConditionValue = 0.0,
            CubicInterpolation.BoundaryCondition rightCondition = CubicInterpolation.BoundaryCondition.SecondDerivative,
            double rightConditionValue = 0.0)
        {
            n_ = n;
            behavior_ = behavior;
            da_ = da;
            monotonic_ = monotonic;
            leftType_ = leftCondition;
            rightType_ = rightCondition;
            leftValue_ = leftConditionValue;
            rightValue_ = rightConditionValue;
            global = true;
            requiredPoints = 3;
        }

        public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin) =>
            new MixedLinearCubicInterpolation(xBegin, xEnd,
                yBegin, n_, behavior_,
                da_, monotonic_,
                leftType_, leftValue_,
                rightType_, rightValue_);

        // fix below
        public bool global { get; set; }
        public int requiredPoints { get; set; }

        private int n_;
        private Behavior behavior_;
        private CubicInterpolation.DerivativeApprox da_;
        private bool monotonic_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;
    }
}