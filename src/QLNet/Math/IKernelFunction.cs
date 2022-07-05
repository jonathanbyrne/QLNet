using JetBrains.Annotations;

namespace QLNet.Math
{
    [PublicAPI]
    public interface IKernelFunction
    {
        double value(double x);
    }
}
