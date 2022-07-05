using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussLegendrePolynomial : GaussJacobiPolynomial
    {
        public GaussLegendrePolynomial() : base(0.0, 0.0)
        {
        }
    }
}
