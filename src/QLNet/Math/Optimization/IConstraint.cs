namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public interface IConstraint
    {
        bool test(Vector param);
        Vector upperBound(Vector parameters);
        Vector lowerBound(Vector parameters);
    }
}