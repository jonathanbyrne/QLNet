using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class JarrowRudd : EqualProbabilitiesBinomialTree<JarrowRudd>, ITreeFactory<JarrowRudd>
    {
        // parameterless constructor is requried for generics
        public JarrowRudd()
        {
        }

        public JarrowRudd(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps)
        {
            // drift removed
            up_ = process.stdDeviation(0.0, x0_, dt_);
        }

        public JarrowRudd factory(StochasticProcess1D process, double end, int steps, double strike) => new JarrowRudd(process, end, steps, strike);
    }
}
