using JetBrains.Annotations;
using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.asian
{
    [PublicAPI]
    public class MakeMCDiscreteArithmeticASEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        private bool antithetic_;
        private bool brownianBridge_;
        private GeneralizedBlackScholesProcess process_;
        private int? samples_, maxSamples_;
        private ulong seed_;
        private double? tolerance_;

        public MakeMCDiscreteArithmeticASEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            brownianBridge_ = true;
            seed_ = 0;
        }

        // conversion to pricing engine
        public IPricingEngine value() =>
            new MCDiscreteArithmeticASEngine<RNG, S>(process_,
                brownianBridge_,
                antithetic_,
                samples_.Value, tolerance_.Value,
                maxSamples_.Value,
                seed_);

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withAntitheticVariate(bool b)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withAntitheticVariate() => withAntitheticVariate(true);

        // named parameters
        public MakeMCDiscreteArithmeticASEngine<RNG, S> withBrownianBridge(bool b)
        {
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withBrownianBridge() => withBrownianBridge(true);

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                "chosen random generator policy " + "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }
    }
}
