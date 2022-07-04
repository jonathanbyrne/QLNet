﻿using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class Cubic : IInterpolationFactory
    {
        private CubicInterpolation.DerivativeApprox da_;
        private bool monotonic_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;

        public Cubic() : this(CubicInterpolation.DerivativeApprox.Kruger, false,
            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0)
        { }

        public Cubic(CubicInterpolation.DerivativeApprox da, bool monotonic,
            CubicInterpolation.BoundaryCondition leftCondition, double leftConditionValue,
            CubicInterpolation.BoundaryCondition rightCondition, double rightConditionValue)
        {
            da_ = da;
            monotonic_ = monotonic;
            leftType_ = leftCondition;
            rightType_ = rightCondition;
            leftValue_ = leftConditionValue;
            rightValue_ = rightConditionValue;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) =>
            new CubicInterpolation(xBegin, size, yBegin, da_, monotonic_, leftType_, leftValue_, rightType_,
                rightValue_);

        public bool global => true;

        public int requiredPoints => 2;
    }
}