namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussHyperbolicPolynomial : GaussianOrthogonalPolynomial
    {
        public override double mu_0() => Const.M_PI;

        public override double alpha(int i) => 0.0;

        public override double beta(int i) => i != 0 ? Const.M_PI_2 * Const.M_PI_2 * i * i : Const.M_PI;

        public override double w(double x) => 1 / System.Math.Cosh(x);
    }
}