using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet
{
    [PublicAPI]
    public interface IDiscretization
    {
        Matrix covariance(StochasticProcess sp, double t0, Vector x0, double dt);

        Matrix diffusion(StochasticProcess sp, double t0, Vector x0, double dt);

        Vector drift(StochasticProcess sp, double t0, Vector x0, double dt);
    }
}
