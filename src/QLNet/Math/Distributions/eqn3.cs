﻿namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class eqn3
    {
        /* Relates to eqn3 Genz 2004 */
        public eqn3(double h, double k, double asr)
        {
            hk_ = h * k;
            hs_ = (h * h + k * k) / 2;
            asr_ = asr;
        }
        public double value(double x)
        {
            var sn = System.Math.Sin(asr_ * (-x + 1) * 0.5);
            return System.Math.Exp((sn * hk_ - hs_) / (1.0 - sn * sn));
        }

        private double hk_, asr_, hs_;

    }
}