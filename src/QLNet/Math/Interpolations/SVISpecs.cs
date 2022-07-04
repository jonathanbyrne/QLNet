using System;
using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    public struct SVISpecs : IModel
    {
        public int dimension() => 5;

        public void defaultValues(List<double?> param, List<bool> paramIsFixed, double forward, double expiryTime, List<double?> addParams)
        {
            if (param[2] == null)
                param[2] = 0.1;
            if (param[3] == null)
                param[3] = -0.4;
            if (param[4] == null)
                param[4] = 0.0;
            if (param[1] == null)
                param[1] = 2.0 / (1.0 + System.Math.Abs(Convert.ToDouble(param[3])));
            if (param[0] == null)
                param[0] = System.Math.Max(0.20 * 0.20 * expiryTime -
                                           (double)param[1] * ((double)param[3] * -(double)param[4] +
                                                               System.Math.Sqrt(-(double)param[4] * -(double)param[4] +
                                                                                (double)param[2] * (double)param[2])),
                    -(double)param[1] * (double)param[2] *
                    System.Math.Sqrt(1.0 - (double)param[3] * (double)param[3]) + eps1());
        }

        public void guess(Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r, List<double?> addParams)
        {
            var j = 0;
            if (!paramIsFixed[2])
                values[2] = r[j++] + eps1();
            if (!paramIsFixed[3])
                values[3] = (2.0 * r[j++] - 1.0) * eps2();
            if (!paramIsFixed[4])
                values[4] = 2.0 * r[j++] - 1.0;
            if (!paramIsFixed[1])
                values[1] = r[j++] * 4.0 / (1.0 + System.Math.Abs(values[3])) * eps2();
            if (!paramIsFixed[0])
                values[0] = r[j++] * expiryTime -
                            eps2() * (values[1] * values[2] *
                                      System.Math.Sqrt(1.0 - values[3] * values[3]));
        }
        public double eps1() => 0.000001;

        public double eps2() => 0.999999;

        public double dilationFactor() => 0.001;

        public Vector inverse(Vector y, List<bool> b, List<double?> c, double d)
        {
            var x = new Vector(5);
            x[2] = System.Math.Sqrt(y[2] - eps1());
            x[3] = System.Math.Asin(y[3] / eps2());
            x[4] = y[4];
            x[1] = System.Math.Tan(y[1] / 4.0 * (1.0 + System.Math.Abs(y[3])) / eps2() * Const.M_PI -
                                   Const.M_PI / 2.0);
            x[0] = System.Math.Sqrt(y[0] - eps1() +
                                    y[1] * y[2] * System.Math.Sqrt(1.0 - y[3] * y[3]));
            return x;
        }
        public Vector direct(Vector x, List<bool> paramIsFixed, List<double?> param, double forward)
        {
            var y = new Vector(5);
            y[2] = x[2] * x[2] + eps1();
            y[3] = System.Math.Sin(x[3]) * eps2();
            y[4] = x[4];
            if (paramIsFixed[1])
                y[1] = Convert.ToDouble(param[1]);
            else
                y[1] = (System.Math.Atan(x[1]) + Const.M_PI / 2.0) / Const.M_PI * eps2() * 4.0 /
                       (1.0 + System.Math.Abs(y[3]));
            if (paramIsFixed[0])
                y[0] = Convert.ToDouble(param[0]);
            else
                y[0] = eps1() + x[0] * x[0] -
                       y[1] * y[2] * System.Math.Sqrt(1.0 - y[3] * y[3]);
            return y;
        }
        public IWrapper instance(double t, double forward, List<double?> param, List<double?> addParams) => new SVIWrapper(t, forward, param, addParams);

        public double weight(double strike, double forward, double stdDev, List<double?> addParams) => Utils.blackFormulaStdDevDerivative(strike, forward, stdDev, 1.0);

        public SVIWrapper modelInstance_ { get; set; }
    }
}