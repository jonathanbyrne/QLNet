using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussGegenbauerIntegration : GaussianQuadrature
    {
        public GaussGegenbauerIntegration(int n, double lambda)
            : base(n, new GaussJacobiPolynomial(lambda - 0.5, lambda - 0.5))
        {
        }
    }
}
