using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;
using QLNet.Math.statistics;
using QLNet.Patterns;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class MakeMCHestonHullWhiteEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        private bool antithetic_, controlVariate_;
        private HybridHestonHullWhiteProcess process_;
        private ulong seed_;
        private int? steps_, stepsPerYear_, samples_, maxSamples_;
        private double? tolerance_;

        public MakeMCHestonHullWhiteEngine(HybridHestonHullWhiteProcess process)
        {
            process_ = process;
            steps_ = null;
            stepsPerYear_ = null;
            samples_ = null;
            maxSamples_ = null;
            antithetic_ = false;
            controlVariate_ = false;
            tolerance_ = null;
            seed_ = 0;
        }

        // conversion to pricing engine
        public IPricingEngine getAsPricingEngine()
        {
            QLNet.Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            QLNet.Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");
            return new MCHestonHullWhiteEngine<RNG, S>(process_,
                steps_,
                stepsPerYear_,
                antithetic_,
                controlVariate_,
                samples_,
                tolerance_,
                maxSamples_,
                seed_);
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            QLNet.Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            QLNet.Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                "chosen random generator policy does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withAntitheticVariate(bool b = true)
        {
            antithetic_ = b;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withControlVariate(bool b = true)
        {
            controlVariate_ = b;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withSamples(int samples)
        {
            QLNet.Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        // named parameters
        public MakeMCHestonHullWhiteEngine<RNG, S> withSteps(int steps)
        {
            steps_ = steps;
            return this;
        }

        public MakeMCHestonHullWhiteEngine<RNG, S> withStepsPerYear(int steps)
        {
            stepsPerYear_ = steps;
            return this;
        }
    }
}
