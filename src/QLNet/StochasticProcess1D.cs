using QLNet.Math;

namespace QLNet
{
    public abstract class StochasticProcess1D : StochasticProcess
    {
        protected new IDiscretization1D discretization_;

        protected StochasticProcess1D()
        {
        }

        protected StochasticProcess1D(IDiscretization1D disc)
        {
            discretization_ = disc;
        }

        // \brief returns the diffusion part of the equation
        public abstract double diffusion(double t, double x);

        //! returns the drift part of the equation, i.e. \f$ \mu(t, x_t) \f$
        public abstract double drift(double t, double x);

        // 1-D stochastic process interface
        //! returns the initial value of the state variable
        public abstract double x0();

        // applies a change to the asset value.
        public virtual double apply(double x0, double dx) => x0 + dx;

        public virtual Vector apply(ref Vector x0, ref Vector dx)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
         QL_REQUIRE(dx.size() == 1, () => "1-D array required");
#endif
            var a = new Vector(1, apply(x0[0], dx[0]));
            return a;
        }

        public override Matrix diffusion(double t, Vector x)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x.size() == 1, () => "1-D array required");
#endif
            var m = new Matrix(1, 1, diffusion(t, x[0]));
            return m;
        }

        public override Vector drift(double t, Vector x)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x.size() == 1, () => "1-D array required");
#endif
            var a = new Vector(1, drift(t, x[0]));
            return a;
        }

        // returns the asset value after a time interval.
        public virtual double evolve(double t0, double x0, double dt, double dw) => apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);

        public virtual Vector evolve(double t0, ref Vector x0, double dt, ref Vector dw)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
         QL_REQUIRE(dw.size() == 1, () => "1-D array required");
#endif
            var a = new Vector(1, evolve(t0, x0[0], dt, dw[0]));
            return a;
        }

        /*! returns the expectation. This method can be
          overridden in derived classes which want to hard-code a
          particular discretization.
      */
        public virtual double expectation(double t0, double x0, double dt) => apply(x0, discretization_.drift(this, t0, x0, dt));

        public override Vector expectation(double t0, Vector x0, double dt)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
            var a = new Vector(1, expectation(t0, x0[0], dt));
            return a;
        }

        //! returns the initial values of the state variables
        public override Vector initialValues()
        {
            var a = new Vector(1, x0());
            return a;
        }

        public override int size() => 1;

        /*! returns the standard deviation. This method can be
          overridden in derived classes which want to hard-code a
          particular discretization.
      */
        public virtual double stdDeviation(double t0, double x0, double dt) => discretization_.diffusion(this, t0, x0, dt);

        public override Matrix stdDeviation(double t0, Vector x0, double dt)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
            var m = new Matrix(1, 1, stdDeviation(t0, x0[0], dt));
            return m;
        }

        /*! returns the variance. This method can be
          overridden in derived classes which want to hard-code a
          particular discretization.
      */
        public virtual double variance(double t0, double x0, double dt) => discretization_.variance(this, t0, x0, dt);

        public virtual Matrix variance(double t0, Vector x0, double dt)
        {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
            var m = new Matrix(1, 1, variance(t0, x0[0], dt));
            return m;
        }
    }
}
