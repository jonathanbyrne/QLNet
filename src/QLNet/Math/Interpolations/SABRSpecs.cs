using System;
using System.Collections.Generic;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Math.Interpolations
{
    public struct SABRSpecs : IModel
    {
        private double shift_;
        private VolatilityType volatilityType_;

        public void defaultValues(List<double?> param, List<bool> b, double forward, double expiryTime, List<double?> addParams)
        {
            shift_ = addParams == null ? 0.0 : Convert.ToDouble(addParams[0]);
            volatilityType_ = addParams == null ? VolatilityType.ShiftedLognormal : addParams[1] > 0.0 ? VolatilityType.Normal : VolatilityType.ShiftedLognormal;
            if (param[1] == null)
            {
                param[1] = 0.5;
            }

            if (param[0] == null)
            {
                // adapt alpha to beta level
                param[0] = 0.2 * (param[1] < 0.9999 && forward + shift_ > 0.0
                    ? System.Math.Pow(forward + shift_
                        , 1.0 - param[1].Value)
                    : 1.0);
            }

            if (param[2] == null)
            {
                param[2] = System.Math.Sqrt(0.4);
            }

            if (param[3] == null)
            {
                param[3] = 0.0;
            }
        }

        public double dilationFactor() => 0.001;

        public int dimension() => 4;

        public Vector direct(Vector x, List<bool> b, List<double?> c, double d)
        {
            var y = new Vector(4);
            y[0] = System.Math.Abs(x[0]) < 5.0
                ? x[0] * x[0] + eps1()
                : 10.0 * System.Math.Abs(x[0]) - 25.0 + eps1();
            y[1] = System.Math.Abs(x[1]) < System.Math.Sqrt(-System.Math.Log(eps1())) && x[1] != 0.0
                ? System.Math.Exp(-(x[1] * x[1]))
                : volatilityType_ == VolatilityType.ShiftedLognormal
                    ? eps1()
                    : 0.0;
            y[2] = System.Math.Abs(x[2]) < 5.0
                ? x[2] * x[2] + eps1()
                : 10.0 * System.Math.Abs(x[2]) - 25.0 + eps1();
            y[3] = System.Math.Abs(x[3]) < 2.5 * Const.M_PI
                ? eps2() * System.Math.Sin(x[3])
                : eps2() * (x[3] > 0.0 ? 1.0 : -1.0);
            return y;
        }

        public double eps1() => .0000001;

        public double eps2() => .9999;

        public void guess(Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r, List<double?> addParams)
        {
            shift_ = addParams == null ? 0.0 : Convert.ToDouble(addParams[0]);
            volatilityType_ = addParams == null ? VolatilityType.ShiftedLognormal : addParams[1] > 0.0 ? VolatilityType.Normal : VolatilityType.ShiftedLognormal;
            var j = 0;
            if (!paramIsFixed[1])
            {
                values[1] = (1.0 - 2E-6) * r[j++] + 1E-6;
            }

            if (!paramIsFixed[0])
            {
                values[0] = (1.0 - 2E-6) * r[j++] + 1E-6; // lognormal vol guess
                // adapt this to beta level
                if (values[1] < 0.999 && forward + shift_ > 0.0)
                {
                    values[0] *= System.Math.Pow(forward + shift_,
                        1.0 - values[1]);
                }
            }

            if (!paramIsFixed[2])
            {
                values[2] = 1.5 * r[j++] + 1E-6;
            }

            if (!paramIsFixed[3])
            {
                values[3] = (2.0 * r[j++] - 1.0) * (1.0 - 1E-6);
            }
        }

        public IWrapper instance(double t, double forward, List<double?> param, List<double?> addParams)
        {
            shift_ = addParams == null ? 0.0 : Convert.ToDouble(addParams[0]);
            volatilityType_ = addParams == null ? VolatilityType.ShiftedLognormal : addParams[1] > 0.0 ? VolatilityType.Normal : VolatilityType.ShiftedLognormal;
            return new SABRWrapper(t, forward, param, addParams);
        }

        public Vector inverse(Vector y, List<bool> b, List<double?> c, double d)
        {
            var x = new Vector(4);
            x[0] = y[0] < 25.0 + eps1()
                ? System.Math.Sqrt(System.Math.Max(eps1(), y[0]) - eps1())
                : (y[0] - eps1() + 25.0) / 10.0;
            x[1] = y[1] == 0.0 ? 0.0 : System.Math.Sqrt(-System.Math.Log(y[1]));
            x[2] = y[2] < 25.0 + eps1()
                ? System.Math.Sqrt(y[2] - eps1())
                : (y[2] - eps1() + 25.0) / 10.0;
            x[3] = System.Math.Asin(y[3] / eps2());
            return x;
        }

        public double weight(double strike, double forward, double stdDev, List<double?> addParams)
        {
            if (Convert.ToDouble(addParams[1]) == 0.0)
            {
                return PricingEngines.Utils.blackFormulaStdDevDerivative(strike, forward, stdDev, 1.0, addParams[0].Value);
            }

            return PricingEngines.Utils.bachelierBlackFormulaStdDevDerivative(strike, forward, stdDev);
        }
    }
}
