using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class BlackScholesLattice<T> : BlackScholesLattice where T : ITree
    {
        public BlackScholesLattice(ITree tree, double riskFreeRate, double end, int steps)
            : base(tree, riskFreeRate, end, steps)
        {
        }
    }
}
