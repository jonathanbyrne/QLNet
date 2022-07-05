using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public interface ITreeFactory<T>
    {
        T factory(StochasticProcess1D process, double end, int steps, double strike);
    }
}
