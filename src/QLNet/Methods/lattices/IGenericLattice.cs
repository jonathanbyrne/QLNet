using QLNet.Math;

namespace QLNet.Methods.lattices
{
    [JetBrains.Annotations.PublicAPI] public interface IGenericLattice
    {
        int size(int i);
        double discount(int i, int j);
        void stepback(int i, Vector values, Vector newValues);
        double underlying(int i, int index);
        int descendant(int i, int index, int branch);
        double probability(int i, int index, int branch);
    }
}