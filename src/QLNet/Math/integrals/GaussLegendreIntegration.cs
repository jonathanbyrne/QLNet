using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussLegendreIntegration : GaussianQuadrature
    {
        public GaussLegendreIntegration(int n)
            : base(n, new GaussJacobiPolynomial(0.0, 0.0))
        {
        }
    }
}
