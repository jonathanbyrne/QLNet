using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class VannaVolgaInterpolationImpl : Interpolation.templateImpl
    {
        private double atmVol_;
        private double dDiscount_;
        private double fDiscount_;
        private double fwd_;
        private List<double> premiaBS;
        private List<double> premiaMKT;
        private double spot_;
        private double T_;
        private List<double> vegas;

        public VannaVolgaInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
            double spot, double dDiscount, double fDiscount, double T)
            : base(xBegin, size, yBegin, VannaVolga.requiredPoints)
        {
            spot_ = spot;
            dDiscount_ = dDiscount;
            fDiscount_ = fDiscount;
            T_ = T;

            premiaBS = new List<double>();
            premiaMKT = new List<double>();
            vegas = new List<double>();

            QLNet.Utils.QL_REQUIRE(size == 3, () => "Vanna Volga Interpolator only interpolates 3 volatilities in strike space");
        }

        public override double derivative(double x)
        {
            QLNet.Utils.QL_FAIL("Vanna Volga derivative not implemented");
            return 0;
        }

        public override double primitive(double x)
        {
            QLNet.Utils.QL_FAIL("Vanna Volga primitive not implemented");
            return 0;
        }

        public override double secondDerivative(double x)
        {
            QLNet.Utils.QL_FAIL("Vanna Volga secondDerivative not implemented");
            return 0;
        }

        public override void update()
        {
            //atmVol should be the second vol
            atmVol_ = yBegin_[1];
            fwd_ = spot_ * fDiscount_ / dDiscount_;
            for (var i = 0; i < 3; i++)
            {
                premiaBS.Add(PricingEngines.Utils.blackFormula(Option.Type.Call, xBegin_[i], fwd_, atmVol_ * System.Math.Sqrt(T_), dDiscount_));
                premiaMKT.Add(PricingEngines.Utils.blackFormula(Option.Type.Call, xBegin_[i], fwd_, yBegin_[i] * System.Math.Sqrt(T_), dDiscount_));
                vegas.Add(vega(xBegin_[i]));
            }
        }

        public override double value(double k)
        {
            var x1 = vega(k) / vegas[0]
                     * (System.Math.Log(xBegin_[1] / k) * System.Math.Log(xBegin_[2] / k))
                     / (System.Math.Log(xBegin_[1] / xBegin_[0]) * System.Math.Log(xBegin_[2] / xBegin_[0]));
            var x2 = vega(k) / vegas[1]
                     * (System.Math.Log(k / xBegin_[0]) * System.Math.Log(xBegin_[2] / k))
                     / (System.Math.Log(xBegin_[1] / xBegin_[0]) * System.Math.Log(xBegin_[2] / xBegin_[1]));
            var x3 = vega(k) / vegas[2]
                     * (System.Math.Log(k / xBegin_[0]) * System.Math.Log(k / xBegin_[1]))
                     / (System.Math.Log(xBegin_[2] / xBegin_[0]) * System.Math.Log(xBegin_[2] / xBegin_[1]));

            var cBS = PricingEngines.Utils.blackFormula(Option.Type.Call, k, fwd_, atmVol_ * System.Math.Sqrt(T_), dDiscount_);
            var c = cBS + x1 * (premiaMKT[0] - premiaBS[0]) + x2 * (premiaMKT[1] - premiaBS[1]) + x3 * (premiaMKT[2] - premiaBS[2]);
            var std = PricingEngines.Utils.blackFormulaImpliedStdDev(Option.Type.Call, k, fwd_, c, dDiscount_);
            return std / System.Math.Sqrt(T_);
        }

        private double vega(double k)
        {
            var d1 = (System.Math.Log(fwd_ / k) + 0.5 * System.Math.Pow(atmVol_, 2.0) * T_) / (atmVol_ * System.Math.Sqrt(T_));
            var norm = new NormalDistribution();
            return spot_ * dDiscount_ * System.Math.Sqrt(T_) * norm.value(d1);
        }
    }
}
