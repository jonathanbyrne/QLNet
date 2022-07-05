using JetBrains.Annotations;

namespace QLNet
{
    [PublicAPI]
    public interface IDiscretization1D
    {
        double diffusion(StochasticProcess1D sp, double t0, double x0, double dt);

        double drift(StochasticProcess1D sp, double t0, double x0, double dt);

        double variance(StochasticProcess1D sp, double t0, double x0, double dt);
    }
}
