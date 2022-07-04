namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussJacobiIntegration : GaussianQuadrature
    {
        public GaussJacobiIntegration(int n, double alpha, double beta)
            : base(n, new GaussJacobiPolynomial(alpha, beta)) { }
    }
}