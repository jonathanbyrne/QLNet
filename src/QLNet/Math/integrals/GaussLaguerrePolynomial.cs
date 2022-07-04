using QLNet.Math.Distributions;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussLaguerrePolynomial : GaussianOrthogonalPolynomial
    {
        private double s_;

        public GaussLaguerrePolynomial() : this(0.0) { }
        public GaussLaguerrePolynomial(double s)
        {
            s_ = s;
            Utils.QL_REQUIRE(s > -1.0, () => "s must be bigger than -1");
        }

        public override double mu_0() => System.Math.Exp(GammaFunction.logValue(s_ + 1));

        public override double alpha(int i) => 2 * i + 1 + s_;

        public override double beta(int i) => i * (i + s_);

        public override double w(double x) => System.Math.Pow(x, s_) * System.Math.Exp(-x);
    }
}