using System.Collections.Generic;
using QLNet.Extensions;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class BackwardflatLinearInterpolationImpl : Interpolation2D.templateImpl
    {
        public BackwardflatLinearInterpolationImpl(List<double> xBegin, int xEnd, List<double> yBegin, int yEnd,
            Matrix zData)
            : base(xBegin, xEnd, yBegin, yEnd, zData)
        {
            calculate();
        }

        public override void calculate() { }

        public override double value(double x, double y)
        {
            var j = locateY(y);
            double z1, z2;
            if (x <= xBegin_[0])
            {
                z1 = zData_[j, 0];
                z2 = zData_[j + 1, 0];
            }
            else
            {
                var i = locateX(x);
                if (x.IsEqual(xBegin_[i]))
                {
                    z1 = zData_[j, i];
                    z2 = zData_[j + 1, i];
                }
                else
                {
                    z1 = zData_[j, i + 1];
                    z2 = zData_[j + 1, i + 1];
                }
            }

            var u = (y - yBegin_[j]) / (yBegin_[j + 1] - yBegin_[j]);

            return (1.0 - u) * z1 + u * z2;

        }

    }
}