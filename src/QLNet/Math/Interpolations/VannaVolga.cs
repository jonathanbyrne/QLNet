using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class VannaVolga
    {
        public const int requiredPoints = 3;
        private double dDiscount_;
        private double fDiscount_;
        private double spot_;
        private double T_;

        public VannaVolga(double spot, double dDiscount, double fDiscount, double T)
        {
            spot_ = spot;
            dDiscount_ = dDiscount;
            fDiscount_ = fDiscount;
            T_ = T;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new VannaVolgaInterpolation(xBegin, size, yBegin, spot_, dDiscount_, fDiscount_, T_);
    }
}
