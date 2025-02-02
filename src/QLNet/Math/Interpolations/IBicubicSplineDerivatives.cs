﻿using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public interface IBicubicSplineDerivatives
    {
        double derivativeX(double x, double y);

        double derivativeXY(double x, double y);

        double derivativeY(double x, double y);

        double secondDerivativeX(double x, double y);

        double secondDerivativeY(double x, double y);
    }
}
