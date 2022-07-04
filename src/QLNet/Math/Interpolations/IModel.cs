using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public interface IModel
    {
        void defaultValues(List<double?> param, List<bool> b, double forward, double expiryTime, List<double?> addParams);
        double dilationFactor();
        int dimension();
        Vector direct(Vector x, List<bool> b, List<double?> c, double d);
        double eps1();
        double eps2();

        void guess(Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r,
            List<double?> addParams);

        IWrapper instance(double t, double forward, List<double?> param, List<double?> addParams);
        Vector inverse(Vector y, List<bool> b, List<double?> c, double d);
        double weight(double strike, double forward, double stdDev, List<double?> addParams);
    }
}