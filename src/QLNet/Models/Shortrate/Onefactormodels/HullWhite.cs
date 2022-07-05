/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Methods.lattices;
using QLNet.Processes;
using QLNet.Termstructures;

namespace QLNet.Models.Shortrate.Onefactormodels
{
    /// <summary>
    ///     Single-factor Hull-White (extended %Vasicek) model class.
    ///     <remarks>
    ///         This class implements the standard single-factor Hull-White model defined by
    ///         dr_t = (\theta(t) - \alpha r_t)dt + \sigma dW_t
    ///         where alpha and sigma are constants.
    ///         calibration results are tested against cached values
    ///         When the term structure is relinked, the r0 parameter of
    ///         the underlying Vasicek model is not updated.
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class HullWhite : Vasicek, ITermStructureConsistentModel
    {
        //! Short-rate dynamics in the Hull-White model
        public new class Dynamics : ShortRateDynamics
        {
            private readonly Parameter fitting_;

            public Dynamics(Parameter fitting, double a, double sigma)
                : base(new OrnsteinUhlenbeckProcess(a, sigma))
            {
                fitting_ = fitting;
            }

            public override double shortRate(double t, double x) => x + fitting_.value(t);

            public override double variable(double t, double r) => r - fitting_.value(t);
        }

        //! Analytical term-structure fitting parameter \f$ \varphi(t) \f$.
        /*! \f$ \varphi(t) \f$ is analytically defined by
            \f[
                \varphi(t) = f(t) + \frac{1}{2}[\frac{\sigma(1-e^{-at})}{a}]^2,
            \f]
            where \f$ f(t) \f$ is the instantaneous forward rate at \f$ t \f$.
        */
        [PublicAPI]
        public class FittingParameter : TermStructureFittingParameter
        {
            private new class Impl : Parameter.Impl
            {
                private readonly double a_;
                private readonly double sigma_;
                private readonly Handle<YieldTermStructure> termStructure_;

                public Impl(Handle<YieldTermStructure> termStructure,
                    double a, double sigma)
                {
                    termStructure_ = termStructure;
                    a_ = a;
                    sigma_ = sigma;
                }

                public override double value(Vector v, double t)
                {
                    var forwardRate =
                        termStructure_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).rate();
                    var temp = a_ < System.Math.Sqrt(Const.QL_EPSILON) ? sigma_ * t : sigma_ * (1.0 - System.Math.Exp(-a_ * t)) / a_;
                    return forwardRate + 0.5 * temp * temp;
                }
            }

            public FittingParameter(Handle<YieldTermStructure> termStructure,
                double a, double sigma)
                : base(new Impl(termStructure, a, sigma))
            {
            }
        }

        private Parameter phi_;

        public HullWhite(Handle<YieldTermStructure> termStructure,
            double a, double sigma)
            : base(termStructure.link.forwardRate(0.0, 0.0, Compounding.Continuous, Frequency.NoFrequency).rate(),
                a, 0.0, sigma)
        {
            termStructure_ = termStructure;
            b_ = arguments_[1] = new NullParameter(); //to change
            lambda_ = arguments_[3] = new NullParameter(); //to change
            generateArguments();
            termStructure.registerWith(update);
        }

        public HullWhite(Handle<YieldTermStructure> termStructure,
            double a)
            : this(termStructure, a, 0.01)
        {
        }

        public HullWhite(Handle<YieldTermStructure> termStructure)
            : this(termStructure, 0.1, 0.01)
        {
        }

        /*! Futures convexity bias (i.e., the difference between
            futures implied rate and forward rate) calculated as in
            G. Kirikos, D. Novak, "Convexity Conundrums", Risk
            Magazine, March 1997.

            \note t and T should be expressed in yearfraction using
                  deposit day counter, F_quoted is futures' market price.
        */
        public static double convexityBias(double futuresPrice,
            double t,
            double T,
            double sigma,
            double a)
        {
            QLNet.Utils.QL_REQUIRE(futuresPrice >= 0.0, () => "negative futures price (" + futuresPrice + ") not allowed");
            QLNet.Utils.QL_REQUIRE(t >= 0.0, () => "negative t (" + t + ") not allowed");
            QLNet.Utils.QL_REQUIRE(T >= t, () => "T (" + T + ") must not be less than t (" + t + ")");
            QLNet.Utils.QL_REQUIRE(sigma >= 0.0, () => "negative sigma (" + sigma + ") not allowed");
            QLNet.Utils.QL_REQUIRE(a >= 0.0, () => "negative a (" + a + ") not allowed");

            var deltaT = T - t;
            var tempDeltaT = (1.0 - System.Math.Exp(-a * deltaT)) / a;
            var halfSigmaSquare = sigma * sigma / 2.0;

            // lambda adjusts for the fact that the underlying is an interest rate
            var lambda = halfSigmaSquare * (1.0 - System.Math.Exp(-2.0 * a * t)) / a *
                         tempDeltaT * tempDeltaT;

            var tempT = (1.0 - System.Math.Exp(-a * t)) / a;

            // phi is the MtM adjustment
            var phi = halfSigmaSquare * tempDeltaT * tempT * tempT;

            // the adjustment
            var z = lambda + phi;

            var futureRate = (100.0 - futuresPrice) / 100.0;
            return (1.0 - System.Math.Exp(-z)) * (futureRate + 1.0 / (T - t));
        }

        public override double discountBondOption(Option.Type type,
            double strike,
            double maturity,
            double bondMaturity)
        {
            var _a = a();
            double v;
            if (_a < System.Math.Sqrt(Const.QL_EPSILON))
            {
                v = sigma() * B(maturity, bondMaturity) * System.Math.Sqrt(maturity);
            }
            else
            {
                v = sigma() * B(maturity, bondMaturity) *
                    System.Math.Sqrt(0.5 * (1.0 - System.Math.Exp(-2.0 * _a * maturity)) / _a);
            }

            var f = termStructure().link.discount(bondMaturity);
            var k = termStructure().link.discount(maturity) * strike;

            return PricingEngines.Utils.blackFormula(type, k, f, v);
        }

        public override ShortRateDynamics dynamics() => new Dynamics(phi_, a(), sigma());

        public override Lattice tree(TimeGrid grid)
        {
            var phi = new TermStructureFittingParameter(termStructure());
            ShortRateDynamics numericDynamics = new Dynamics(phi, a(), sigma());
            var trinomial = new TrinomialTree(numericDynamics.process(), grid);
            var numericTree = new ShortRateTree(trinomial, numericDynamics, grid);
            var impl =
                (TermStructureFittingParameter.NumericalImpl)phi.implementation();
            impl.reset();
            for (var i = 0; i < grid.size() - 1; i++)
            {
                var discountBond = termStructure().link.discount(grid[i + 1]);
                var statePrices = numericTree.statePrices(i);
                var size = numericTree.size(i);
                var dt = numericTree.timeGrid().dt(i);
                var dx = trinomial.dx(i);
                var x = trinomial.underlying(i, 0);
                var value = 0.0;
                for (var j = 0; j < size; j++)
                {
                    value += statePrices[j] * System.Math.Exp(-x * dt);
                    x += dx;
                }

                value = System.Math.Log(value / discountBond) / dt;
                impl.setvalue(grid[i], value);
            }

            return numericTree;
        }

        protected override double A(double t, double T)
        {
            var discount1 = termStructure().link.discount(t);
            var discount2 = termStructure().link.discount(T);
            var forward = termStructure().link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).rate();
            var temp = sigma() * B(t, T);
            var value = B(t, T) * forward - 0.25 * temp * temp * B(0.0, 2.0 * t);
            return System.Math.Exp(value) * discount2 / discount1;
        }

        //a voir pour le sealed sinon pb avec classe mere vasicek et constructeur class Hullwithe
        protected override void generateArguments()
        {
            phi_ = new FittingParameter(termStructure(), a(), sigma());
        }

        #region ITermStructureConsistentModel

        public Handle<YieldTermStructure> termStructure() => termStructure_;

        public Handle<YieldTermStructure> termStructure_ { get; set; }

        #endregion
    }
}
