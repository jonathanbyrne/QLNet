using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class MixedLinearCubic
    {
        private Behavior behavior_;
        private CubicInterpolation.DerivativeApprox da_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;
        private bool monotonic_;
        private int n_;

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

        // fix below
        public bool global { get; set; }

        public int requiredPoints { get; set; }

        public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin) =>
            new MixedLinearCubicInterpolation(xBegin, xEnd,
                yBegin, n_, behavior_,
                da_, monotonic_,
                leftType_, leftValue_,
                rightType_, rightValue_);
    }
}
