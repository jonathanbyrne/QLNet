using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussHermiteIntegration : GaussianQuadrature
    {
        public GaussHermiteIntegration(int n, double mu = 0.0)
            : base(n, new GaussHermitePolynomial(mu))
        {
        }
    }
}
