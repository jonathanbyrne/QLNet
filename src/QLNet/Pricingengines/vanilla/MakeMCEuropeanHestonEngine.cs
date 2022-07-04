using QLNet.Math.randomnumbers;
using QLNet.Math.statistics;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class MakeMCEuropeanHestonEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
        public MakeMCEuropeanHestonEngine(HestonProcess process)
        {
            process_ = process;
            antithetic_ = false;
            steps_ = null;
            stepsPerYear_ = null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            seed_ = 0;


        }
        // named parameters
        public MakeMCEuropeanHestonEngine<RNG, S> withSteps(int steps)
        {
            Utils.QL_REQUIRE(stepsPerYear_ == null, () => "number of steps per year already set");
            steps_ = steps;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withStepsPerYear(int steps)
        {
            Utils.QL_REQUIRE(steps_ == null, () => "number of steps already set");
            stepsPerYear_ = steps;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withSamples(int samples)
        {
            Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
            samples_ = samples;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withAbsoluteTolerance(double tolerance)
        {
            Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
            Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () => "chosen random generator policy does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withMaxSamples(int samples)
        {
            maxSamples_ = samples;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withSeed(ulong seed)
        {
            seed_ = seed;
            return this;
        }

        public MakeMCEuropeanHestonEngine<RNG, S> withAntitheticVariate(bool b = true)
        {
            antithetic_ = b;
            return this;
        }

        // conversion to pricing engine
        public IPricingEngine getAsPricingEngine()
        {
            Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
            return new MCEuropeanHestonEngine<RNG, S>(process_,
                steps_,
                stepsPerYear_,
                antithetic_,
                samples_, tolerance_,
                maxSamples_,
                seed_);
        }


        private HestonProcess process_;
        private bool antithetic_;
        private int? steps_, stepsPerYear_, samples_, maxSamples_;
        private double? tolerance_;
        private ulong seed_;
    }
}