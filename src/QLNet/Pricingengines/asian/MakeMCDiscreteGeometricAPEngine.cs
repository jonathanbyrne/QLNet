using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Patterns;
using QLNet.Processes;

namespace QLNet.PricingEngines.asian
{
    [PublicAPI]
    public class MakeMCDiscreteGeometricAPEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        private bool antithetic_, controlVariate_;
        private bool brownianBridge_;
        private GeneralizedBlackScholesProcess process_;
        private ulong seed_;
        private int? steps_, samples_, maxSamples_;
        private double? tolerance_;

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

        // conversion to pricing engine
        public IPricingEngine value()
        {
            QLNet.Utils.QL_REQUIRE(steps_ != null, () => "max number of steps per year not given");
            return new MCDiscreteGeometricAPEngine<RNG, S>(process_,
                steps_.Value,
                brownianBridge_,
                antithetic_, controlVariate_,
                samples_.Value, tolerance_.Value,
                maxSamples_.Value,
                seed_);
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate() => withAntitheticVariate(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge(bool b)
        {
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge() => withBrownianBridge(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate(bool b)
        {
            controlVariate_ = b;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withControlVariate() => withControlVariate(true);

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSamples(int samples)
        {
            QLNet.Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        // named parameters
        public MakeMCDiscreteGeometricAPEngine<RNG, S> withStepsPerYear(int maxSteps)
        {
            steps_ = maxSteps;
            return this;
        }

        public MakeMCDiscreteGeometricAPEngine<RNG, S> withTolerance(double tolerance)
        {
            QLNet.Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            QLNet.Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                "chosen random generator policy " + "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }
    }
}
