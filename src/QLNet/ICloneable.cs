using JetBrains.Annotations;

namespace QLNet
{
    [PublicAPI]
    public interface ICloneable
    {
        object Clone();
    }
}
