using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussHyperbolicIntegration : GaussianQuadrature
    {
        public GaussHyperbolicIntegration(int n)
            : base(n, new GaussHyperbolicPolynomial())
        {
        }
    }
}
