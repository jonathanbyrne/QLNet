namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussChebyshevIntegration : GaussianQuadrature
    {
        public GaussChebyshevIntegration(int n)
            : base(n, new GaussJacobiPolynomial(-0.5, -0.5)) { }
    }
}