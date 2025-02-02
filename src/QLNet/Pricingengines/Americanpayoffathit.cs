/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

namespace QLNet.PricingEngines
{
    //! Analytic formula for American exercise payoff at-hit options
    //! \todo calculate greeks
    [PublicAPI]
    public class AmericanPayoffAtHit
    {
        private double alpha_;
        private double beta_;
        private double D1_;
        private double D2_;
        private double DalphaDd1_;
        private double DbetaDd2_;
        private double discount_;
        private double dividendDiscount_;
        private double forward_;
        private bool inTheMoney_;
        private double K_;
        private double lambda_;
        private double log_H_S_;
        private double mu_;
        private double muMinusLambda_;
        private double muPlusLambda_;
        private double spot_;
        private double stdDev_;
        private double strike_;
        private double variance_;
        private double X_;

        public AmericanPayoffAtHit(double spot, double discount, double dividendDiscount, double variance, StrikedTypePayoff payoff)
        {
            spot_ = spot;
            discount_ = discount;
            dividendDiscount_ = dividendDiscount;
            variance_ = variance;

            QLNet.Utils.QL_REQUIRE(spot_ > 0.0, () => "positive spot value required");
            QLNet.Utils.QL_REQUIRE(discount_ > 0.0, () => "positive discount required");
            QLNet.Utils.QL_REQUIRE(dividendDiscount_ > 0.0, () => "positive dividend discount required");
            QLNet.Utils.QL_REQUIRE(variance_ >= 0.0, () => "negative variance not allowed");

            stdDev_ = System.Math.Sqrt(variance_);

            var type = payoff.optionType();
            strike_ = payoff.strike();

            log_H_S_ = System.Math.Log(strike_ / spot_);

            double n_d1;
            double n_d2;
            double cum_d1_;
            double cum_d2_;
            if (variance_ >= Const.QL_EPSILON)
            {
                if (discount_.IsEqual(0.0) && dividendDiscount_.IsEqual(0.0))
                {
                    mu_ = -0.5;
                    lambda_ = 0.5;
                }
                else if (discount_.IsEqual(0.0))
                {
                    QLNet.Utils.QL_FAIL("null discount not handled yet");
                }
                else
                {
                    mu_ = System.Math.Log(dividendDiscount_ / discount_) / variance_ - 0.5;
                    lambda_ = System.Math.Sqrt(mu_ * mu_ - 2.0 * System.Math.Log(discount_) / variance_);
                }

                D1_ = log_H_S_ / stdDev_ + lambda_ * stdDev_;
                D2_ = D1_ - 2.0 * lambda_ * stdDev_;
                var f = new CumulativeNormalDistribution();
                cum_d1_ = f.value(D1_);
                cum_d2_ = f.value(D2_);
                n_d1 = f.derivative(D1_);
                n_d2 = f.derivative(D2_);
            }
            else
            {
                // not tested yet
                mu_ = System.Math.Log(dividendDiscount_ / discount_) / variance_ - 0.5;
                lambda_ = System.Math.Sqrt(mu_ * mu_ - 2.0 * System.Math.Log(discount_) / variance_);
                if (log_H_S_ > 0)
                {
                    cum_d1_ = 1.0;
                    cum_d2_ = 1.0;
                }
                else
                {
                    cum_d1_ = 0.0;
                    cum_d2_ = 0.0;
                }

                n_d1 = 0.0;
                n_d2 = 0.0;
            }

            switch (type)
            {
                // up-and-in cash-(at-hit)-or-nothing option
                // a.k.a. american call with cash-or-nothing payoff
                case QLNet.Option.Type.Call:
                    if (strike_ > spot_)
                    {
                        alpha_ = 1.0 - cum_d1_; // N(-d1)
                        DalphaDd1_ = -n_d1; // -n( d1)
                        beta_ = 1.0 - cum_d2_; // N(-d2)
                        DbetaDd2_ = -n_d2; // -n( d2)
                    }
                    else
                    {
                        alpha_ = 0.5;
                        DalphaDd1_ = 0.0;
                        beta_ = 0.5;
                        DbetaDd2_ = 0.0;
                    }

                    break;
                // down-and-in cash-(at-hit)-or-nothing option
                // a.k.a. american put with cash-or-nothing payoff
                case QLNet.Option.Type.Put:
                    if (strike_ < spot_)
                    {
                        alpha_ = cum_d1_; // N(d1)
                        DalphaDd1_ = n_d1; // n(d1)
                        beta_ = cum_d2_; // N(d2)
                        DbetaDd2_ = n_d2; // n(d2)
                    }
                    else
                    {
                        alpha_ = 0.5;
                        DalphaDd1_ = 0.0;
                        beta_ = 0.5;
                        DbetaDd2_ = 0.0;
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                    break;
            }

            muPlusLambda_ = mu_ + lambda_;
            muMinusLambda_ = mu_ - lambda_;
            inTheMoney_ = type == QLNet.Option.Type.Call && strike_ < spot_ || type == QLNet.Option.Type.Put && strike_ > spot_;

            if (inTheMoney_)
            {
                forward_ = 1.0;
                X_ = 1.0;
            }
            else
            {
                forward_ = System.Math.Pow(strike_ / spot_, muPlusLambda_);
                X_ = System.Math.Pow(strike_ / spot_, muMinusLambda_);
            }

            // Binary Cash-Or-Nothing payoff?
            if (payoff is CashOrNothingPayoff coo)
            {
                K_ = coo.cashPayoff();
            }

            // Binary Asset-Or-Nothing payoff?

            if (payoff is AssetOrNothingPayoff aoo)
            {
                if (inTheMoney_)
                {
                    K_ = spot_;
                }
                else
                {
                    K_ = aoo.strike();
                }
            }
        }

        public double delta()
        {
            var tempDelta = -spot_ * stdDev_;
            var DalphaDs = DalphaDd1_ / tempDelta;
            var DbetaDs = DbetaDd2_ / tempDelta;

            double DforwardDs;
            double DXDs;
            if (inTheMoney_)
            {
                DforwardDs = 0.0;
                DXDs = 0.0;
            }
            else
            {
                DforwardDs = -muPlusLambda_ * forward_ / spot_;
                DXDs = -muMinusLambda_ * X_ / spot_;
            }

            return K_ * (DalphaDs * forward_ + alpha_ * DforwardDs + DbetaDs * X_ + beta_ * DXDs);
        }

        public double gamma()
        {
            var tempDelta = -spot_ * stdDev_;
            var DalphaDs = DalphaDd1_ / tempDelta;
            var DbetaDs = DbetaDd2_ / tempDelta;
            var D2alphaDs2 = -DalphaDs / spot_ * (1 - D1_ / stdDev_);
            var D2betaDs2 = -DbetaDs / spot_ * (1 - D2_ / stdDev_);

            double DforwardDs;
            double DXDs;
            double D2forwardDs2;
            double D2XDs2;
            if (inTheMoney_)
            {
                DforwardDs = 0.0;
                DXDs = 0.0;
                D2forwardDs2 = 0.0;
                D2XDs2 = 0.0;
            }
            else
            {
                DforwardDs = -muPlusLambda_ * forward_ / spot_;
                DXDs = -muMinusLambda_ * X_ / spot_;
                D2forwardDs2 = muPlusLambda_ * forward_ / (spot_ * spot_) * (1 + muPlusLambda_);
                D2XDs2 = muMinusLambda_ * X_ / (spot_ * spot_) * (1 + muMinusLambda_);
            }

            return K_ * (D2alphaDs2 * forward_ + DalphaDs * DforwardDs + DalphaDs * DforwardDs + alpha_ * D2forwardDs2 + D2betaDs2 * X_ + DbetaDs * DXDs + DbetaDs * DXDs + beta_ * D2XDs2);
        }

        public double rho(double maturity)
        {
            QLNet.Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

            // actually D.Dr / T
            var DalphaDr = -DalphaDd1_ / (lambda_ * stdDev_) * (1.0 + mu_);
            var DbetaDr = DbetaDd2_ / (lambda_ * stdDev_) * (1.0 + mu_);
            double DforwardDr;
            double DXDr;
            if (inTheMoney_)
            {
                DforwardDr = 0.0;
                DXDr = 0.0;
            }
            else
            {
                DforwardDr = forward_ * (1.0 + (1.0 + mu_) / lambda_) * log_H_S_ / variance_;
                DXDr = X_ * (1.0 - (1.0 + mu_) / lambda_) * log_H_S_ / variance_;
            }

            return maturity * K_ * (DalphaDr * forward_ + alpha_ * DforwardDr + DbetaDr * X_ + beta_ * DXDr);
        }

        // inline definitions
        public double value() => K_ * (forward_ * alpha_ + X_ * beta_);
    }
}
