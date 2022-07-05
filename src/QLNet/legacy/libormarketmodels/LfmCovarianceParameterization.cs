using QLNet.Math;
using QLNet.Math.integrals;

namespace QLNet.legacy.libormarketmodels
{
    public abstract class LfmCovarianceParameterization
    {
        protected int factors_;
        protected int size_;

        protected LfmCovarianceParameterization(int size, int factors)
        {
            size_ = size;
            factors_ = factors;
        }

        public abstract Matrix diffusion(double t);

        public abstract Matrix diffusion(double t, Vector x);

        public virtual Matrix covariance(double t) => covariance(t, null);

        public virtual Matrix covariance(double t, Vector x)
        {
            var sigma = diffusion(t, x);
            var result = sigma * Matrix.transpose(sigma);
            return result;
        }

        public int factors() => factors_;

        public virtual Matrix integratedCovariance(double t, Vector x = null)
        {
            // this implementation is not intended for production.
            // because it is too slow and too inefficient.
            // This method is useful for testing and R&D.
            // Please overload the method within derived classes.

            Utils.QL_REQUIRE(x == null, () => "can not handle given x here");

            var tmp = new Matrix(size_, size_, 0.0);

            for (var i = 0; i < size_; ++i)
            {
                for (var j = 0; j <= i; ++j)
                {
                    var helper = new Var_Helper(this, i, j);
                    var integrator = new GaussKronrodAdaptive(1e-10, 10000);
                    for (var k = 0; k < 64; ++k)
                    {
                        tmp[i, j] += integrator.value(helper.value, k * t / 64.0, (k + 1) * t / 64.0);
                    }

                    tmp[j, i] = tmp[i, j];
                }
            }

            return tmp;
        }

        public int size() => size_;
    }
}
