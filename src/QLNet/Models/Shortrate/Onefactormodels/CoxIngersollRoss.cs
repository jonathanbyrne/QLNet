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
using QLNet.Math.Distributions;
using QLNet.Methods.lattices;

namespace QLNet.Models.Shortrate.Onefactormodels
{
    /// <summary>
    ///     Cox-Ingersoll-Ross model class.
    ///     <remarks>
    ///         This class implements the Cox-Ingersoll-Ross model defined by
    ///         dr_t = k(\theta - r_t)dt + \sqrt{r_t}\sigma dW_t .
    ///         This class was not tested enough to guarantee its functionality.
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class CoxIngersollRoss : OneFactorAffineModel
    {
        //! %Dynamics of the short-rate under the Cox-Ingersoll-Ross model
        /*! The state variable \f$ y_t \f$ will here be the square-root of the
            short-rate.
        */
        [PublicAPI]
        public class Dynamics : ShortRateDynamics
        {
            public Dynamics(double theta,
                double k,
                double sigma,
                double x0)
                : base(new HelperProcess(theta, k, sigma, System.Math.Sqrt(x0)))
            {
            }

            public override double shortRate(double d, double y) => y * y;

            public override double variable(double d, double r) => System.Math.Sqrt(r);
        }

        [PublicAPI]
        public class HelperProcess : StochasticProcess1D
        {
            private double y0_, theta_, k_, sigma_;

            public HelperProcess(double theta, double k, double sigma, double y0)
            {
                y0_ = y0;
                theta_ = theta;
                k_ = k;
                sigma_ = sigma;
            }

            public override double diffusion(double d1, double d2) => 0.5 * sigma_;

            public override double drift(double d, double y) =>
                (0.5 * theta_ * k_ - 0.125 * sigma_ * sigma_) / y
                - 0.5 * k_ * y;

            public override double x0() => y0_;
        }

        private Parameter k_;
        private Parameter r0_;
        private Parameter sigma_;
        private Parameter theta_;

        public CoxIngersollRoss(double r0 = 0.05,
            double theta = 0.1,
            double k = 0.1,
            double sigma = 0.1)
            : base(4)
        {
            theta_ = arguments_[0];
            k_ = arguments_[1];
            sigma_ = arguments_[2];
            r0_ = arguments_[3];
        }

        public override double discountBondOption(Option.Type type,
            double strike,
            double maturity,
            double bondMaturity)
        {
            QLNet.Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");
            var discountT = discountBond(0.0, maturity, x0());
            var discountS = discountBond(0.0, bondMaturity, x0());

            if (maturity < Const.QL_EPSILON)
            {
                switch (type)
                {
                    case Option.Type.Call:
                        return System.Math.Max(discountS - strike, 0.0);
                    case Option.Type.Put:
                        return System.Math.Max(strike - discountS, 0.0);
                    default:
                        QLNet.Utils.QL_FAIL("unsupported option ExerciseType");
                        break;
                }
            }

            var sigma2 = sigma() * sigma();
            var h = System.Math.Sqrt(k() * k() + 2.0 * sigma2);
            var b = B(maturity, bondMaturity);

            var rho = 2.0 * h / (sigma2 * (System.Math.Exp(h * maturity) - 1.0));
            var psi = (k() + h) / sigma2;

            var df = 4.0 * k() * theta() / sigma2;
            var ncps = 2.0 * rho * rho * x0() * System.Math.Exp(h * maturity) / (rho + psi + b);
            var ncpt = 2.0 * rho * rho * x0() * System.Math.Exp(h * maturity) / (rho + psi);

            var chis = new NonCentralChiSquareDistribution(df, ncps);
            var chit = new NonCentralChiSquareDistribution(df, ncpt);

            var z = System.Math.Log(A(maturity, bondMaturity) / strike) / b;
            var call = discountS * chis.value(2.0 * z * (rho + psi + b)) -
                       strike * discountT * chit.value(2.0 * z * (rho + psi));

            if (type == Option.Type.Call)
            {
                return call;
            }

            return call - discountS + strike * discountT;
        }

        public override ShortRateDynamics dynamics() => new Dynamics(theta(), k(), sigma(), x0());

        public override Lattice tree(TimeGrid grid)
        {
            var trinomial = new TrinomialTree(dynamics().process(), grid, true);
            return new ShortRateTree(trinomial, dynamics(), grid);
        }

        protected override double A(double t, double T)
        {
            var sigma2 = sigma() * sigma();
            var h = System.Math.Sqrt(k() * k() + 2.0 * sigma2);
            var numerator = 2.0 * h * System.Math.Exp(0.5 * (k() + h) * (T - t));
            var denominator = 2.0 * h + (k() + h) * (System.Math.Exp((T - t) * h) - 1.0);
            var value = System.Math.Log(numerator / denominator) *
                2.0 * k() * theta() / sigma2;
            return System.Math.Exp(value);
        }

        protected override double B(double t, double T)
        {
            var h = System.Math.Sqrt(k() * k() + 2.0 * sigma() * sigma());
            var temp = System.Math.Exp((T - t) * h) - 1.0;
            var numerator = 2.0 * temp;
            var denominator = 2.0 * h + (k() + h) * temp;
            var value = numerator / denominator;
            return value;
        }

        protected double k() => k_.value(0.0);

        protected double sigma() => sigma_.value(0.0);

        protected double theta() => theta_.value(0.0);

        protected double x0() => r0_.value(0.0);
    }
}
