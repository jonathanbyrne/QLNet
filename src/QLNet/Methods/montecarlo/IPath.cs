using JetBrains.Annotations;

namespace QLNet.Methods.montecarlo
{
    [PublicAPI]
    public interface IPath : ICloneable
    {
        int length();
    }
}
