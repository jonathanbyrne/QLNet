namespace QLNet.Methods.lattices
{
    [JetBrains.Annotations.PublicAPI] public interface ITreeFactory<T>
    {
        T factory(StochasticProcess1D process, double end, int steps, double strike);
    }
}