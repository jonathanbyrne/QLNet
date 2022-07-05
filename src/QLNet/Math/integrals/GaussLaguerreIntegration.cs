using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussLaguerreIntegration : GaussianQuadrature
    {
        public GaussLaguerreIntegration(int n, double s = 0.0)
            : base(n, new GaussLaguerrePolynomial(s))
        {
        }
    }
}
