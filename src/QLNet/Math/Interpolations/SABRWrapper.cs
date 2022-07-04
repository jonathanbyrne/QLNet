using System.Collections.Generic;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class SABRWrapper : IWrapper
    {
        public SABRWrapper(double t, double forward, List<double?> param, List<double?> addParams)
        {
            t_ = t;
            forward_ = forward;
            params_ = param;
            shift_ = addParams == null ? 0.0 : addParams[0];
            volatilityType_ = addParams == null ? VolatilityType.ShiftedLognormal : addParams[1] > 0.0 ? VolatilityType.Normal : VolatilityType.ShiftedLognormal;
            approximationModel_ = addParams == null ? SabrApproximationModel.Hagan2002 : (SabrApproximationModel)addParams[2];

            if (volatilityType_ == VolatilityType.ShiftedLognormal)
                Utils.QL_REQUIRE(forward_ + shift_ > 0.0, () => "forward+shift must be positive: "
                                                                + forward_ + " with shift "
                                                                + shift_.Value + " not allowed");

            Utils.validateSabrParameters(param[0].Value, param[1].Value, param[2].Value, param[3].Value);
        }
        public double volatility(double x)
        {
            switch (volatilityType_)
            {
                case VolatilityType.ShiftedLognormal:
                    return Utils.shiftedSabrVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value, shift_.Value, approximationModel_);
                case VolatilityType.Normal:
                    return Utils.shiftedSabrNormalVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value, shift_.Value);
                default:
                    return Utils.sabrVolatility(x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value);
            }
        }

        private double t_, forward_;
        private double? shift_;
        private List<double?> params_;
        private VolatilityType volatilityType_;
        private SabrApproximationModel approximationModel_;
    }
}