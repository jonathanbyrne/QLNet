namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussChebyshev2ndIntegration : GaussianQuadrature
    {
        public GaussChebyshev2ndIntegration(int n)
            : base(n, new GaussJacobiPolynomial(0.5, 0.5)) { }
    }
}