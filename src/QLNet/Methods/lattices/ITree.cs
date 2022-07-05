using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public interface ITree
    {
        int descendant(int x, int index, int branch);

        double probability(int x, int y, int z);

        int size(int i);

        double underlying(int i, int index);
    }
}
