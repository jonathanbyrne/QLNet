using JetBrains.Annotations;

namespace QLNet.Patterns
{
    [PublicAPI]
    public interface IObserver
    {
        void update();
    }
}
