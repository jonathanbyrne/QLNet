using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class NonLinearLeastSquare
    {
        //! required accuracy of the solver
        private double accuracy_;
        private double bestAccuracy_;
        //constraint
        private Constraint c_;
        //! Exit flag of the optimization process
        private int exitFlag_;
        private Vector initialValue_;
        //! maximum and real number of iterations
        private int maxIterations_;
        //! Optimization method
        private OptimizationMethod om_;
        //! least square residual norm
        private double resnorm_;
        //! solution vector
        private Vector results_;

        //! Default constructor
        public NonLinearLeastSquare(Constraint c, double accuracy)
            : this(c, accuracy, 100)
        {
        }

        public NonLinearLeastSquare(Constraint c)
            : this(c, 1e-4, 100)
        {
        }

        public NonLinearLeastSquare(Constraint c, double accuracy, int maxiter)
        {
            exitFlag_ = -1;
            accuracy_ = accuracy;
            maxIterations_ = maxiter;
            om_ = new ConjugateGradient();
            c_ = c;
        }

        //! Default constructor
        public NonLinearLeastSquare(Constraint c, double accuracy, int maxiter, OptimizationMethod om)
        {
            exitFlag_ = -1;
            accuracy_ = accuracy;
            maxIterations_ = maxiter;
            om_ = om;
            c_ = c;
        }

        //! return exit flag
        public int exitFlag() => exitFlag_;

        //! return last function value
        public double lastValue() => bestAccuracy_;

        //! Solve least square problem using numerix solver
        public Vector perform(ref LeastSquareProblem lsProblem)
        {
            var eps = accuracy_;

            // wrap the least square problem in an optimization function
            var lsf = new LeastSquareFunction(lsProblem);

            // define optimization problem
            var P = new Problem(lsf, c_, initialValue_);

            // minimize
            var ec = new EndCriteria(maxIterations_, System.Math.Min(maxIterations_ / 2, 100), eps, eps, eps);
            exitFlag_ = (int)om_.minimize(P, ec);

            results_ = P.currentValue();
            resnorm_ = P.functionValue();
            bestAccuracy_ = P.functionValue();

            return results_;
        }

        //! return the least square residual norm
        public double residualNorm() => resnorm_;

        //! return the results
        public Vector results() => results_;

        public void setInitialValue(Vector initialValue)
        {
            initialValue_ = initialValue;
        }
    }
}
