namespace QLNet.Methods.lattices
{
    [JetBrains.Annotations.PublicAPI] public class EqualJumpsBinomialTree<T> : BinomialTree<T>
    {
        protected double dx_, pu_, pd_;

        // parameterless constructor is requried for generics
        public EqualJumpsBinomialTree()
        { }

        public EqualJumpsBinomialTree(StochasticProcess1D process, double end, int steps)
            : base(process, end, steps)
        { }

        public override double underlying(int i, int index)
        {
            long j = 2 * index - i;
            // exploiting equal jump and the x0_ tree centering
            return x0_ * System.Math.Exp(j * dx_);
        }

        public override double probability(int x, int y, int branch) => branch == 1 ? pu_ : pd_;
    }
}