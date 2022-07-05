using JetBrains.Annotations;

namespace QLNet
{
    [PublicAPI]
    public interface IValue
    {
        double value(double v);
    }
}
