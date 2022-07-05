using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class AmericanPathPricer : IEarlyExercisePathPricer<IPath, double>
    {
        protected Payoff payoff_;
        protected double scalingValue_;
        protected List<Func<double, double>> v_ = new List<Func<double, double>>();

        public AmericanPathPricer(Payoff payoff, int polynomOrder, LsmBasisSystem.PolynomType polynomType)
        {
            scalingValue_ = 1;
            payoff_ = payoff;
            v_ = LsmBasisSystem.pathBasisSystem(polynomOrder, polynomType);

            QLNet.Utils.QL_REQUIRE(polynomType == LsmBasisSystem.PolynomType.Monomial
                                   || polynomType == LsmBasisSystem.PolynomType.Laguerre
                                   || polynomType == LsmBasisSystem.PolynomType.Hermite
                                   || polynomType == LsmBasisSystem.PolynomType.Hyperbolic
                                   || polynomType == LsmBasisSystem.PolynomType.Chebyshev2th, () => "insufficient polynom ExerciseType");

            // the payoff gives an additional value
            v_.Add(this.payoff);

            if (payoff_ is StrikedTypePayoff strikePayoff)
            {
                scalingValue_ /= strikePayoff.strike();
            }
        }

        public List<Func<double, double>> basisSystem() => v_;

        // scale values of the underlying to increase numerical stability
        public double state(IPath path, int t) => (path as Path)[t] * scalingValue_;

        public double value(IPath path, int t) => payoff(state(path, t));

        protected double payoff(double state) => payoff_.value(state / scalingValue_);
    }
}
