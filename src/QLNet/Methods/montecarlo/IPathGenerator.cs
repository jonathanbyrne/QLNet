using JetBrains.Annotations;

namespace QLNet.Methods.montecarlo
{
    [PublicAPI]
    public interface IPathGenerator<GSG>
    {
        Sample<IPath> antithetic();

        Sample<IPath> next();
    }
}
