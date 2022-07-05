using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Processes;

namespace QLNet.PricingEngines.barrier
{
    [PublicAPI]
    public class MakeMCBarrierEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        protected bool brownianBridge_, antithetic_, biased_;
        protected GeneralizedBlackScholesProcess process_;
        protected int? steps_, stepsPerYear_, samples_, maxSamples_;
        protected double? tolerance_;
        private ulong seed_;

        public MakeMCBarrierEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            brownianBridge_ = false;
            antithetic_ = false;
            biased_ = false;
            steps_ = null;
            stepsPerYear_ = null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            seed_ = 0;
        }

        // conversion to pricing engine
        public IPricingEngine getAsPricingEngine()
        {
            QLNet.Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            QLNet.Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");
            return new MCBarrierEngine<RNG, S>(process_,
                steps_,
                stepsPerYear_,
                brownianBridge_,
                antithetic_,
                samples_,
                tolerance_,
                maxSamples_,
                biased_,
                seed_);
        }

        public MakeMCBarrierEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            QLNet.Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            QLNet.Utils.QL_REQUIRE(new RNG().allowsErrorEstimate > 0, () => "chosen random generator policy does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withAntitheticVariate(bool b = true)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withBias(bool b = true)
        {
            biased_ = b;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withBrownianBridge(bool b = true)
        {
            brownianBridge_ = b;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withSamples(int samples)
        {
            QLNet.Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        // named parameters
        public MakeMCBarrierEngine<RNG, S> withSteps(int steps)
        {
            steps_ = steps;
            return this;
        }

        public MakeMCBarrierEngine<RNG, S> withStepsPerYear(int steps)
        {
            stepsPerYear_ = steps;
            return this;
        }
    }
}
