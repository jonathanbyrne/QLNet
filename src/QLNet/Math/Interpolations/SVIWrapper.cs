using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class SVIWrapper : IWrapper
    {
        private List<double?> params_;
        private double t_, forward_;

        public SVIWrapper(double t, double forward, List<double?> param, List<double?> addParams)
        {
            t_ = t;
            forward_ = forward;
            params_ = param;
            Termstructures.Volatility.Utils.checkSviParameters(param[0].Value, param[1].Value, param[2].Value, param[3].Value, param[4].Value);
        }

        public double volatility(double x) => Termstructures.Volatility.Utils.sviVolatility(x, forward_, t_, params_);
    }
}
