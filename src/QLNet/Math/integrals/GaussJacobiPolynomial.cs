using QLNet.Extensions;
using QLNet.Math.Distributions;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussJacobiPolynomial : GaussianOrthogonalPolynomial
    {
        private double alpha_;
        private double beta_;

        public GaussJacobiPolynomial(double alpha, double beta)
        {
            alpha_ = alpha;
            beta_ = beta;

            Utils.QL_REQUIRE(alpha_ + beta_ > -2.0, () => "alpha+beta must be bigger than -2");
            Utils.QL_REQUIRE(alpha_ > -1.0, () => "alpha must be bigger than -1");
            Utils.QL_REQUIRE(beta_ > -1.0, () => "beta  must be bigger than -1");
        }

        public override double mu_0() =>
            System.Math.Pow(2.0, alpha_ + beta_ + 1)
            * System.Math.Exp(GammaFunction.logValue(alpha_ + 1)
                              + GammaFunction.logValue(beta_ + 1)
                              - GammaFunction.logValue(alpha_ + beta_ + 2));

        public override double alpha(int i)
        {
            var num = beta_ * beta_ - alpha_ * alpha_;
            var denom = (2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_ + 2);

            if (denom.IsEqual(0.0))
            {
                if (num.IsNotEqual(0.0))
                {
                    Utils.QL_FAIL("can't compute a_k for jacobi integration");
                }
                else
                {
                    // l'Hospital
                    num = 2 * beta_;
                    denom = 2 * (2.0 * i + alpha_ + beta_ + 1);

                    Utils.QL_REQUIRE(denom.IsNotEqual(0.0), () => "can't compute a_k for jacobi integration");
                }
            }

            return num / denom;
        }
        public override double beta(int i)
        {
            var num = 4.0 * i * (i + alpha_) * (i + beta_) * (i + alpha_ + beta_);
            var denom = (2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_)
                                                   * ((2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_) - 1);

            if (denom.IsEqual(0.0))
            {
                if (num.IsNotEqual(0.0))
                {
                    Utils.QL_FAIL("can't compute b_k for jacobi integration");
                }
                else
                {
                    // l'Hospital
                    num = 4.0 * i * (i + beta_) * (2.0 * i + 2 * alpha_ + beta_);
                    denom = 2.0 * (2.0 * i + alpha_ + beta_);
                    denom *= denom - 1;
                    Utils.QL_REQUIRE(denom.IsNotEqual(0.0), () => "can't compute b_k for jacobi integration");
                }
            }
            return num / denom;
        }
        public override double w(double x) => System.Math.Pow(1 - x, alpha_) * System.Math.Pow(1 + x, beta_);
    }
}