using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class LogLinearInterpolation : Interpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */

        public LogLinearInterpolation(List<double> xBegin, int size, List<double> yBegin)
        {
            impl_ = new LogInterpolationImpl<Linear>(xBegin, size, yBegin);
            impl_.update();
        }
    }
}
