using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public interface IGenericLattice
    {
        int descendant(int i, int index, int branch);

        double discount(int i, int j);

        double probability(int i, int index, int branch);

        int size(int i);

        void stepback(int i, Vector values, Vector newValues);

        double underlying(int i, int index);
    }
}
