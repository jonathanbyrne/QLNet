namespace QLNet.Methods.lattices
{
    [JetBrains.Annotations.PublicAPI] public interface ITree
    {
        int size(int i);
        int descendant(int x, int index, int branch);
        double underlying(int i, int index);
        double probability(int x, int y, int z);
    }
}