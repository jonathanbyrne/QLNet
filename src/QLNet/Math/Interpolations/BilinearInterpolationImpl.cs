using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class BilinearInterpolationImpl : Interpolation2D.templateImpl
    {
        public BilinearInterpolationImpl(List<double> xBegin, int xSize,
            List<double> yBegin, int ySize,
            Matrix zData)
            : base(xBegin, xSize, yBegin, ySize, zData)
        {
            calculate();
        }

        public override void calculate()
        {
        }

        public override double value(double x, double y)
        {
            int i = locateX(x), j = locateY(y);

            var z1 = zData_[j, i];
            var z2 = zData_[j, i + 1];
            var z3 = zData_[j + 1, i];
            var z4 = zData_[j + 1, i + 1];

            var t = (x - xBegin_[i]) /
                    (xBegin_[i + 1] - xBegin_[i]);
            var u = (y - yBegin_[j]) /
                    (yBegin_[j + 1] - yBegin_[j]);

            return (1.0 - t) * (1.0 - u) * z1 + t * (1.0 - u) * z2
                                              + (1.0 - t) * u * z3 + t * u * z4;
        }
    }
}
