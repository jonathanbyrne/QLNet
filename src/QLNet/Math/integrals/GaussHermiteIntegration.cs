namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussHermiteIntegration : GaussianQuadrature
    {
        public GaussHermiteIntegration(int n, double mu = 0.0)
            : base(n, new GaussHermitePolynomial(mu)) { }
    }
}