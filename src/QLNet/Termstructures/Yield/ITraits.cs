using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public interface ITraits<T>
    {
        //
        double discountImpl(Interpolation i, double t);

        double forwardImpl(Interpolation i, double t);

        double guess(int i, InterpolatedCurve c, bool validData, int first); // possible constraints based on previous values

        Date initialDate(T c); // start of curve data

        double initialValue(T c); // value at reference date

        int maxIterations(); // upper bound for convergence loop

        double maxValueAfter(int i, InterpolatedCurve c, bool validData, int first); // update with new guess

        double minValueAfter(int i, InterpolatedCurve c, bool validData, int first);

        void updateGuess(List<double> data, double discount, int i);

        double zeroYieldImpl(Interpolation i, double t);
    }
}
