using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Termstructures.Volatility;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class SABRWrapper : IWrapper
    {
        private SabrApproximationModel approximationModel_;
        private List<double?> params_;
        private double? shift_;
        private double t_, forward_;
        private VolatilityType volatilityType_;

        public SABRWrapper(double t, double forward, List<double?> param, List<double?> addParams)
        {
            t_ = t;
            forward_ = forward;
            params_ = param;
            shift_ = addParams == null ? 0.0 : addParams[0];
            volatilityType_ = addParams == null ? VolatilityType.ShiftedLognormal : addParams[1] > 0.0 ? VolatilityType.Normal : VolatilityType.ShiftedLognormal;
            approximationModel_ = addParams == null ? SabrApproximationModel.Hagan2002 : (SabrApproximationModel)addParams[2];

            if (volatilityType_ == VolatilityType.ShiftedLognormal)
            {
                QLNet.Utils.QL_REQUIRE(forward_ + shift_ > 0.0, () => "forward+shift must be positive: "
                                                                               + forward_ + " with shift "
                                                                               + shift_.Value + " not allowed");
            }

            Termstructures.Volatility.Utils.validateSabrParameters(param[0].Value, param[1].Value, param[2].Value, param[3].Value);
        }

        public double volatility(double x)
        {
            switch (volatilityType_)
            {
                case VolatilityType.ShiftedLognormal:
                    return Termstructures.Volatility.Utils.shiftedSabrVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value, shift_.Value, approximationModel_);
                case VolatilityType.Normal:
                    return Termstructures.Volatility.Utils.shiftedSabrNormalVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value, shift_.Value);
                default:
                    return Termstructures.Volatility.Utils.sabrVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value);
            }
        }
    }
}
