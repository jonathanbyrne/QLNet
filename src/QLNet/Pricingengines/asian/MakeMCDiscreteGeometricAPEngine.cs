using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.asian
{
    [JetBrains.Annotations.PublicAPI] public class MakeMCDiscreteGeometricAPEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        public MakeMCDiscreteGeometricAPEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            controlVariate_ = false;
            steps_ = null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            brownianBridge_ = true;
            seed_ = 0;
        }

        // named parameters
        public MakeMCDiscreteGeometricAPEngine<RNG, S> withStepsPerYear(int maxSteps)
        {
            steps_ = maxSteps;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge(bool b)
        {
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge() => withBrownianBridge(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                "chosen random generator policy " + "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate() => withAntitheticVariate(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate(bool b)
        {
            controlVariate_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate() => withControlVariate(true);

        // conversion to pricing engine
        public IPricingEngine value()
        {
            Utils.QL_REQUIRE(steps_ != null, () => "max number of steps per year not given");
            return new MCDiscreteGeometricAPEngine<RNG, S>(process_,
                steps_.Value,
                brownianBridge_,
                antithetic_, controlVariate_,
                samples_.Value, tolerance_.Value,
                maxSamples_.Value,
                seed_);
        }

        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_, controlVariate_;
        private int? steps_, samples_, maxSamples_;
        private double? tolerance_;
        private bool brownianBridge_;
        private ulong seed_;
    }
}