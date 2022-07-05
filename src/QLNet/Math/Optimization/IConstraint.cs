using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public interface IConstraint
    {
        Vector lowerBound(Vector parameters);

        bool test(Vector param);

        Vector upperBound(Vector parameters);
    }
}
