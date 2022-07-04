using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class LogLinearInterpolation : Interpolation
    {
        /*! \pre the \f$ x \f$ values must be sorted. */

        public LogLinearInterpolation(List<double> xBegin, int size, List<double> yBegin)
        {
            impl_ = new LogInterpolationImpl<Linear>(xBegin, size, yBegin);
            impl_.update();
        }
    }
}