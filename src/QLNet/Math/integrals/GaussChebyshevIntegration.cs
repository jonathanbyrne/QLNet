using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussChebyshevIntegration : GaussianQuadrature
    {
        public GaussChebyshevIntegration(int n)
            : base(n, new GaussJacobiPolynomial(-0.5, -0.5))
        {
        }
    }
}
