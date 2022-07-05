using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussChebyshev2ndPolynomial : GaussJacobiPolynomial
    {
        public GaussChebyshev2ndPolynomial() : base(0.5, 0.5)
        {
        }
    }
}
