using JetBrains.Annotations;
using QLNet.Math.Distributions;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussHermitePolynomial : GaussianOrthogonalPolynomial
    {
        private double mu_;

        public GaussHermitePolynomial() : this(0.0)
        {
        }

        public GaussHermitePolynomial(double mu)
        {
            mu_ = mu;
            QLNet.Utils.QL_REQUIRE(mu > -0.5, () => "mu must be bigger than -0.5");
        }

        public override double alpha(int i) => 0.0;

        public override double beta(int i) => i % 2 != 0 ? i / 2.0 + mu_ : i / 2.0;

        public override double mu_0() => System.Math.Exp(GammaFunction.logValue(mu_ + 0.5));

        public override double w(double x) => System.Math.Pow(System.Math.Abs(x), 2 * mu_) * System.Math.Exp(-x * x);
    }
}
