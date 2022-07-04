using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class SVIWrapper : IWrapper
    {
        public SVIWrapper(double t, double forward, List<double?> param, List<double?> addParams)
        {
            t_ = t;
            forward_ = forward;
            params_ = param;
            Utils.checkSviParameters(param[0].Value, param[1].Value, param[2].Value, param[3].Value, param[4].Value);
        }
        public double volatility(double x) => Utils.sviVolatility(x, forward_, t_, params_);

        private double t_, forward_;
        private List<double?> params_;
    }
}