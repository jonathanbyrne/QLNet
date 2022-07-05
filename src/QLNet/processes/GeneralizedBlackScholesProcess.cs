using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Time;

namespace QLNet.Processes
{
    [PublicAPI]
    public class GeneralizedBlackScholesProcess : StochasticProcess1D
    {
        private Handle<BlackVolTermStructure> blackVolatility_;
        private RelinkableHandle<LocalVolTermStructure> localVolatility_ = new RelinkableHandle<LocalVolTermStructure>();
        private Handle<YieldTermStructure> riskFreeRate_, dividendYield_;
        private bool updated_, isStrikeIndependent_;
        private Handle<Quote> x0_;

        public GeneralizedBlackScholesProcess(Handle<Quote> x0, Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS, Handle<BlackVolTermStructure> blackVolTS, IDiscretization1D disc = null)
            : base(disc ?? new EulerDiscretization())
        {
            x0_ = x0;
            riskFreeRate_ = riskFreeTS;
            dividendYield_ = dividendTS;
            blackVolatility_ = blackVolTS;
            updated_ = false;

            x0_.registerWith(update);
            riskFreeRate_.registerWith(update);
            dividendYield_.registerWith(update);
            blackVolatility_.registerWith(update);
        }

        public GeneralizedBlackScholesProcess(Handle<Quote> x0, Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS, Handle<BlackVolTermStructure> blackVolTS,
            RelinkableHandle<LocalVolTermStructure> localVolTS, IDiscretization1D disc = null)
            : base(disc ?? new EulerDiscretization())
        {
            x0_ = x0;
            riskFreeRate_ = riskFreeTS;
            dividendYield_ = dividendTS;
            blackVolatility_ = blackVolTS;
            localVolatility_ = localVolTS != null
                ? localVolTS.empty() ? new RelinkableHandle<LocalVolTermStructure>() : localVolTS
                : new RelinkableHandle<LocalVolTermStructure>();
            updated_ = !localVolatility_.empty();

            x0_.registerWith(update);
            riskFreeRate_.registerWith(update);
            dividendYield_.registerWith(update);
            blackVolatility_.registerWith(update);
            localVolatility_.registerWith(update);
        }

        public override double apply(double x0, double dx) => x0 * System.Math.Exp(dx);

        public Handle<BlackVolTermStructure> blackVolatility() => blackVolatility_;

        /*! \todo revise extrapolation */
        public override double diffusion(double t, double x) => localVolatility().link.localVol(t, x, true);

        public Handle<YieldTermStructure> dividendYield() => dividendYield_;

        /*! \todo revise extrapolation */
        public override double drift(double t, double x)
        {
            var sigma = diffusion(t, x);
            // we could be more anticipatory if we know the right dt for which the drift will be used
            var t1 = t + 0.0001;
            return riskFreeRate_.link.forwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true).rate()
                   - dividendYield_.link.forwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true).rate()
                   - 0.5 * sigma * sigma;
        }

        public override double evolve(double t0, double x0, double dt, double dw)
        {
            localVolatility(); // trigger update
            if (isStrikeIndependent_)
            {
                // exact value for curves
                var var = variance(t0, x0, dt);
                var drift = (riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                 Frequency.NoFrequency, true).value() -
                             dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                 Frequency.NoFrequency, true).value()) *
                    dt - 0.5 * var;
                return apply(x0, System.Math.Sqrt(var) * dw + drift);
            }

            return apply(x0, discretization_.drift(this, t0, x0, dt) + stdDeviation(t0, x0, dt) * dw);
        }

        /*! \warning raises a "not implemented" exception.  It should
               be rewritten to return the expectation E(S) of
               the process, not exp(E(log S)).
        */
        public override double expectation(double t0, double x0, double dt)
        {
            localVolatility(); // trigger update
            if (isStrikeIndependent_)
            {
                // exact value for curves
                return x0 *
                       System.Math.Exp(dt * (riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                                 Frequency.NoFrequency, true).value() -
                                             dividendYield_.link.forwardRate(
                                                 t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true).value()));
            }

            QLNet.Utils.QL_FAIL("not implemented");
            return 0;
        }

        public Handle<LocalVolTermStructure> localVolatility()
        {
            if (!updated_)
            {
                isStrikeIndependent_ = true;

                // constant Black vol?
                if (blackVolatility().link is BlackConstantVol constVol)
                {
                    // ok, the local vol is constant too.
                    localVolatility_.linkTo(new LocalConstantVol(constVol.referenceDate(),
                        constVol.blackVol(0.0, x0_.link.value()),
                        constVol.dayCounter()));
                    updated_ = true;
                    return localVolatility_;
                }

                // ok, so it's not constant. Maybe it's strike-independent?
                if (blackVolatility().link is BlackVarianceCurve volCurve)
                {
                    // ok, we can use the optimized algorithm
                    localVolatility_.linkTo(new LocalVolCurve(new Handle<BlackVarianceCurve>(volCurve)));
                    updated_ = true;
                    return localVolatility_;
                }

                // ok, so it's strike-dependent. Never mind.
                localVolatility_.linkTo(new LocalVolSurface(blackVolatility_, riskFreeRate_, dividendYield_,
                    x0_.link.value()));
                updated_ = true;
                isStrikeIndependent_ = false;
                return localVolatility_;
            }

            return localVolatility_;
        }

        public Handle<YieldTermStructure> riskFreeRate() => riskFreeRate_;

        public Handle<Quote> stateVariable() => x0_;

        public override double stdDeviation(double t0, double x0, double dt)
        {
            localVolatility(); // trigger update
            if (isStrikeIndependent_)
            {
                // exact value for curves
                return System.Math.Sqrt(variance(t0, x0, dt));
            }

            return discretization_.diffusion(this, t0, x0, dt);
        }

        public override double time(Date d) => riskFreeRate_.link.dayCounter().yearFraction(riskFreeRate_.link.referenceDate(), d);

        public override void update()
        {
            updated_ = false;
            base.update();
        }

        public override double variance(double t0, double x0, double dt)
        {
            localVolatility(); // trigger update
            if (isStrikeIndependent_)
            {
                // exact value for curves
                return blackVolatility_.link.blackVariance(t0 + dt, 0.01) -
                       blackVolatility_.link.blackVariance(t0, 0.01);
            }

            return discretization_.variance(this, t0, x0, dt);
        }

        public override double x0() => x0_.link.value();
    }
}
