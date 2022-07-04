using QLNet.Math;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public interface IDiscretization
    {
        Vector drift(StochasticProcess sp, double t0, Vector x0, double dt);
        Matrix diffusion(StochasticProcess sp, double t0, Vector x0, double dt);
        Matrix covariance(StochasticProcess sp, double t0, Vector x0, double dt);
    }
}