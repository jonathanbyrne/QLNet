using JetBrains.Annotations;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public interface IBootStrap<T>
    {
        void calculate();

        void setup(T ts);
    }
}
