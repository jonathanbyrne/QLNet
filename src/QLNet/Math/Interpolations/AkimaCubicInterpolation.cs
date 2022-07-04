﻿using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class AkimaCubicInterpolation : CubicInterpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public AkimaCubicInterpolation(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin,
                DerivativeApprox.Akima, false,
                BoundaryCondition.SecondDerivative, 0.0,
                BoundaryCondition.SecondDerivative, 0.0)
        { }
    }
}