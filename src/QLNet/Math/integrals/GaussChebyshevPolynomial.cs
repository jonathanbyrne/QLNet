using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussChebyshevPolynomial : GaussJacobiPolynomial
    {
        public GaussChebyshevPolynomial() : base(-0.5, -0.5)
        {
        }
    }
}
