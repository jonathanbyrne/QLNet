using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussJacobiIntegration : GaussianQuadrature
    {
        public GaussJacobiIntegration(int n, double alpha, double beta)
            : base(n, new GaussJacobiPolynomial(alpha, beta))
        {
        }
    }
}
