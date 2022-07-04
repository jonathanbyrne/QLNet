namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussHyperbolicIntegration : GaussianQuadrature
    {
        public GaussHyperbolicIntegration(int n)
            : base(n, new GaussHyperbolicPolynomial()) { }
    }
}