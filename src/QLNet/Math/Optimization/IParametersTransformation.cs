using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public interface IParametersTransformation
    {
        Vector direct(Vector x);

        Vector inverse(Vector x);
    }
}
