using QLNet.Math.Solvers1d;

namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class InverseNonCentralCumulativeChiSquareDistribution
    {
        protected NonCentralCumulativeChiSquareDistribution nonCentralDist_;
        protected double guess_;
        protected int maxEvaluations_;
        protected double accuracy_;

        public InverseNonCentralCumulativeChiSquareDistribution(double df, double ncp,
            int maxEvaluations = 10,
            double accuracy = 1e-8)
        {
            nonCentralDist_ = new NonCentralCumulativeChiSquareDistribution(df, ncp);
            guess_ = df + ncp;
            maxEvaluations_ = maxEvaluations;
            accuracy_ = accuracy;
        }

        public double value(double x)
        {
            // first find the right side of the interval
            var upper = guess_;
            var evaluations = maxEvaluations_;
            while (nonCentralDist_.value(upper) < x && evaluations > 0)
            {
                upper *= 2.0;
                --evaluations;
            }

            // use a Brent solver for the rest
            var solver = new Brent();
            ISolver1d f = new MinFinder(nonCentralDist_, x);
            solver.setMaxEvaluations(evaluations);

            return solver.solve(f,
                accuracy_, 0.75 * upper,
                evaluations == maxEvaluations_ ? 0.0 : 0.5 * upper,
                upper);
        }

        protected class MinFinder : ISolver1d
        {
            protected NonCentralCumulativeChiSquareDistribution nonCentralDist_;
            protected double x_;

            public MinFinder(NonCentralCumulativeChiSquareDistribution nonCentralDist, double x)
            {
                nonCentralDist_ = nonCentralDist;
                x_ = x;
            }

            public override double value(double y) => x_ - nonCentralDist_.value(y);
        }
    }
}