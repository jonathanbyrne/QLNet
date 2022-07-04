using System.Collections.Generic;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public interface ITraits<T>
    {
        Date initialDate(T c);      // start of curve data
        double initialValue(T c);     // value at reference date
        double guess(int i, InterpolatedCurve c, bool validData, int first);    // possible constraints based on previous values
        double minValueAfter(int i, InterpolatedCurve c, bool validData, int first);
        double maxValueAfter(int i, InterpolatedCurve c, bool validData, int first);     // update with new guess
        void updateGuess(List<double> data, double discount, int i);
        int maxIterations();                          // upper bound for convergence loop

        //
        double discountImpl(Interpolation i, double t);
        double zeroYieldImpl(Interpolation i, double t);
        double forwardImpl(Interpolation i, double t);
    }
}