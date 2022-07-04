using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class VannaVolga
    {
        public VannaVolga(double spot, double dDiscount, double fDiscount, double T)
        {
            spot_ = spot;
            dDiscount_ = dDiscount;
            fDiscount_ = fDiscount;
            T_ = T;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new VannaVolgaInterpolation(xBegin, size, yBegin, spot_, dDiscount_, fDiscount_, T_);

        public const int requiredPoints = 3;

        private double spot_;
        private double dDiscount_;
        private double fDiscount_;
        private double T_;
    }
}