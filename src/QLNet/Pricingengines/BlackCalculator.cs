/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Patterns;

namespace QLNet.PricingEngines
{
    //! Black 1976 calculator class
    /*! \bug When the variance is null, division by zero occur during
             the calculation of delta, delta forward, gamma, gamma
             forward, rho, dividend rho, vega, and strike sensitivity.
    */
    [PublicAPI]
    public class BlackCalculator
    {
        private class Calculator : IAcyclicVisitor
        {
            private readonly BlackCalculator black_;

            public Calculator(BlackCalculator black)
            {
                black_ = black;
            }

            public void visit(object o)
            {
                var types = new[] { o.GetType() };
                var methodInfo = QLNet.Utils.GetMethodInfo(this, "visit", types);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(this, new[] { o });
                }
            }

            public void visit(Payoff p)
            {
                QLNet.Utils.QL_FAIL("unsupported payoff ExerciseType: " + p.name());
            }

            public void visit(PlainVanillaPayoff p)
            {
                // Nothing to do here
            }

            public void visit(CashOrNothingPayoff payoff)
            {
                black_.alpha_ = black_.DalphaDd1_ = 0.0;
                black_.X_ = payoff.cashPayoff();
                black_.DXDstrike_ = 0.0;
                switch (payoff.optionType())
                {
                    case QLNet.Option.Type.Call:
                        black_.beta_ = black_.cum_d2_;
                        black_.DbetaDd2_ = black_.n_d2_;
                        break;
                    case QLNet.Option.Type.Put:
                        black_.beta_ = 1.0 - black_.cum_d2_;
                        black_.DbetaDd2_ = -black_.n_d2_;
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                        break;
                }
            }

            public void visit(AssetOrNothingPayoff payoff)
            {
                black_.beta_ = black_.DbetaDd2_ = 0.0;
                switch (payoff.optionType())
                {
                    case QLNet.Option.Type.Call:
                        black_.alpha_ = black_.cum_d1_;
                        black_.DalphaDd1_ = black_.n_d1_;
                        break;
                    case QLNet.Option.Type.Put:
                        black_.alpha_ = 1.0 - black_.cum_d1_;
                        black_.DalphaDd1_ = -black_.n_d1_;
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                        break;
                }
            }

            public void visit(GapPayoff payoff)
            {
                black_.X_ = payoff.secondStrike();
                black_.DXDstrike_ = 0.0;
            }
        }

        protected double strike_, forward_, stdDev_, discount_, variance_;
        private double D1_, D2_, alpha_, beta_, DalphaDd1_, DbetaDd2_;
        private double n_d1_, cum_d1_, n_d2_, cum_d2_;
        private double X_, DXDs_, DXDstrike_;

        public BlackCalculator(StrikedTypePayoff payoff, double forward, double stdDev, double discount)
        {
            strike_ = payoff.strike();
            forward_ = forward;
            stdDev_ = stdDev;
            discount_ = discount;
            variance_ = stdDev * stdDev;

            QLNet.Utils.QL_REQUIRE(forward > 0.0, () => "positive forward value required: " + forward + " not allowed");
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "non-negative standard deviation required: " + stdDev + " not allowed");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () => "positive discount required: " + discount + " not allowed");

            if (stdDev_ >= Const.QL_EPSILON)
            {
                if (strike_.IsEqual(0.0))
                {
                    n_d1_ = 0.0;
                    n_d2_ = 0.0;
                    cum_d1_ = 1.0;
                    cum_d2_ = 1.0;
                }
                else
                {
                    D1_ = System.Math.Log(forward / strike_) / stdDev_ + 0.5 * stdDev_;
                    D2_ = D1_ - stdDev_;
                    var f = new CumulativeNormalDistribution();
                    cum_d1_ = f.value(D1_);
                    cum_d2_ = f.value(D2_);
                    n_d1_ = f.derivative(D1_);
                    n_d2_ = f.derivative(D2_);
                }
            }
            else
            {
                if (forward > strike_)
                {
                    cum_d1_ = 1.0;
                    cum_d2_ = 1.0;
                }
                else
                {
                    cum_d1_ = 0.0;
                    cum_d2_ = 0.0;
                }

                n_d1_ = 0.0;
                n_d2_ = 0.0;
            }

            X_ = strike_;
            DXDstrike_ = 1.0;

            // the following one will probably disappear as soon as
            // super-share will be properly handled
            DXDs_ = 0.0;

            // this part is always executed.
            // in case of plain-vanilla payoffs, it is also the only part
            // which is executed.
            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    alpha_ = cum_d1_; //  N(d1)
                    DalphaDd1_ = n_d1_; //  n(d1)
                    beta_ = -cum_d2_; // -N(d2)
                    DbetaDd2_ = -n_d2_; // -n(d2)
                    break;
                case QLNet.Option.Type.Put:
                    alpha_ = -1.0 + cum_d1_; // -N(-d1)
                    DalphaDd1_ = n_d1_; //  n( d1)
                    beta_ = 1.0 - cum_d2_; //  N(-d2)
                    DbetaDd2_ = -n_d2_; // -n( d2)
                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                    break;
            }

            // now dispatch on ExerciseType.

            var calc = new Calculator(this);
            payoff.accept(calc);
        }

        public double alpha() => alpha_;

        public double beta() => beta_;

        /*! Sensitivity to change in the underlying spot price. */
        public virtual double delta(double spot)
        {
            QLNet.Utils.QL_REQUIRE(spot > 0.0, () => "positive spot value required: " + spot + " not allowed");

            var DforwardDs = forward_ / spot;

            var temp = stdDev_ * spot;
            var DalphaDs = DalphaDd1_ / temp;
            var DbetaDs = DbetaDd2_ / temp;
            var temp2 = DalphaDs * forward_ + alpha_ * DforwardDs
                                            + DbetaDs * X_ + beta_ * DXDs_;

            return discount_ * temp2;
        }

        /*! Sensitivity to change in the underlying forward price. */
        public double deltaForward()
        {
            var temp = stdDev_ * forward_;
            var DalphaDforward = DalphaDd1_ / temp;
            var DbetaDforward = DbetaDd2_ / temp;
            var temp2 = DalphaDforward * forward_ + alpha_
                                                  + DbetaDforward * X_; // DXDforward = 0.0

            return discount_ * temp2;
        }

        /*! Sensitivity to dividend/growth rate. */
        public double dividendRho(double maturity)
        {
            QLNet.Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

            // actually DalphaDq / T
            var DalphaDq = -DalphaDd1_ / stdDev_;
            var DbetaDq = -DbetaDd2_ / stdDev_;

            var temp = DalphaDq * forward_ - alpha_ * forward_ + DbetaDq * X_;

            return maturity * discount_ * temp;
        }

        /*! Sensitivity in percent to a percent change in the
            underlying spot price. */
        public virtual double elasticity(double spot)
        {
            var val = value();
            var del = delta(spot);
            if (val > Const.QL_EPSILON)
            {
                return del / val * spot;
            }

            if (System.Math.Abs(del) < Const.QL_EPSILON)
            {
                return 0.0;
            }

            if (del > 0.0)
            {
                return double.MaxValue;
            }

            return double.MinValue;
        }

        /*! Sensitivity in percent to a percent change in the
            underlying forward price. */
        public double elasticityForward()
        {
            var val = value();
            var del = deltaForward();
            if (val > Const.QL_EPSILON)
            {
                return del / val * forward_;
            }

            if (System.Math.Abs(del) < Const.QL_EPSILON)
            {
                return 0.0;
            }

            if (del > 0.0)
            {
                return double.MaxValue;
            }

            return double.MinValue;
        }

        /*! Second order derivative with respect to change in the
            underlying spot price. */
        public virtual double gamma(double spot)
        {
            QLNet.Utils.QL_REQUIRE(spot > 0.0, () => "positive spot value required: " + spot + " not allowed");

            var DforwardDs = forward_ / spot;

            var temp = stdDev_ * spot;
            var DalphaDs = DalphaDd1_ / temp;
            var DbetaDs = DbetaDd2_ / temp;

            var D2alphaDs2 = -DalphaDs / spot * (1 + D1_ / stdDev_);
            var D2betaDs2 = -DbetaDs / spot * (1 + D2_ / stdDev_);

            var temp2 = D2alphaDs2 * forward_ + 2.0 * DalphaDs * DforwardDs
                                              + D2betaDs2 * X_ + 2.0 * DbetaDs * DXDs_;

            return discount_ * temp2;
        }

        /*! Second order derivative with respect to change in the
            underlying forward price. */
        public double gammaForward()
        {
            var temp = stdDev_ * forward_;
            var DalphaDforward = DalphaDd1_ / temp;
            var DbetaDforward = DbetaDd2_ / temp;

            var D2alphaDforward2 = -DalphaDforward / forward_ * (1 + D1_ / stdDev_);
            var D2betaDforward2 = -DbetaDforward / forward_ * (1 + D2_ / stdDev_);

            var temp2 = D2alphaDforward2 * forward_ + 2.0 * DalphaDforward
                                                    + D2betaDforward2 * X_; // DXDforward = 0.0

            return discount_ * temp2;
        }

        /*! Probability of being in the money in the asset martingale
            measure, i.e. N(d1).
            It is a risk-neutral probability, not the real world one.
        */
        public double itmAssetProbability() => cum_d1_;

        /*! Probability of being in the money in the bond martingale
            measure, i.e. N(d2).
            It is a risk-neutral probability, not the real world one.
        */
        public double itmCashProbability() => cum_d2_;

        /*! Sensitivity to discounting rate. */
        public double rho(double maturity)
        {
            QLNet.Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

            // actually DalphaDr / T
            var DalphaDr = DalphaDd1_ / stdDev_;
            var DbetaDr = DbetaDd2_ / stdDev_;
            var temp = DalphaDr * forward_ + alpha_ * forward_ + DbetaDr * X_;

            return maturity * (discount_ * temp - value());
        }

        /*! Sensitivity to strike. */
        public double strikeSensitivity()
        {
            var temp = stdDev_ * strike_;
            var DalphaDstrike = -DalphaDd1_ / temp;
            var DbetaDstrike = -DbetaDd2_ / temp;

            var temp2 = DalphaDstrike * forward_ + DbetaDstrike * X_ + beta_ * DXDstrike_;

            return discount_ * temp2;
        }

        /*! Sensitivity to time to maturity. */
        public virtual double theta(double spot, double maturity)
        {
            if (maturity.IsEqual(0.0))
            {
                return 0.0;
            }

            QLNet.Utils.QL_REQUIRE(maturity > 0.0, () => "non negative maturity required: " + maturity + " not allowed");

            return -(System.Math.Log(discount_) * value()
                     + System.Math.Log(forward_ / spot) * spot * delta(spot)
                     + 0.5 * variance_ * spot * spot * gamma(spot)) / maturity;
        }

        /*! Sensitivity to time to maturity per day,
            assuming 365 day per year. */
        public virtual double thetaPerDay(double spot, double maturity) => theta(spot, maturity) / 365.0;

        public double value()
        {
            var result = discount_ * (forward_ * alpha_ + X_ * beta_);
            return result;
        }

        /*! Sensitivity to volatility. */
        public double vega(double maturity)
        {
            QLNet.Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

            var temp = System.Math.Log(strike_ / forward_) / variance_;
            // actually DalphaDsigma / SQRT(T)
            var DalphaDsigma = DalphaDd1_ * (temp + 0.5);
            var DbetaDsigma = DbetaDd2_ * (temp - 0.5);

            var temp2 = DalphaDsigma * forward_ + DbetaDsigma * X_;

            return discount_ * System.Math.Sqrt(maturity) * temp2;
        }
    }
}
