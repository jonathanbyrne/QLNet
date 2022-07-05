using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussGegenbauerPolynomial : GaussJacobiPolynomial
    {
        public GaussGegenbauerPolynomial(double lambda) : base(lambda - 0.5, lambda - 0.5)
        {
        }
    }
}
