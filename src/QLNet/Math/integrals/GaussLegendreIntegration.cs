namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussLegendreIntegration : GaussianQuadrature
    {
        public GaussLegendreIntegration(int n)
            : base(n, new GaussJacobiPolynomial(0.0, 0.0)) { }
    }
}