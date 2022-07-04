namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public interface IParametersTransformation
    {
        Vector direct(Vector x);
        Vector inverse(Vector x);
    }
}