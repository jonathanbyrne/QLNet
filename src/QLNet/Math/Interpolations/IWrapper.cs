using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public interface IWrapper
    {
        double volatility(double x);
    }
}
