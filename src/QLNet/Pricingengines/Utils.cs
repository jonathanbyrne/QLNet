//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Math.Distributions;
using QLNet.Math.Solvers1d;

namespace QLNet.PricingEngines
{
    public partial class Utils
    {
        private class BlackImpliedStdDevHelper : ISolver1d
        {
            private readonly double halfOptionType_;
            private readonly CumulativeNormalDistribution N_;
            private readonly double signedStrike_;
            private readonly double signedForward_;
            private readonly double undiscountedBlackPrice_;
            private readonly double signedMoneyness_;

            public BlackImpliedStdDevHelper(QLNet.Option.Type optionType,
                double strike,
                double forward,
                double undiscountedBlackPrice,
                double displacement = 0.0)
            {
                halfOptionType_ = 0.5 * (int)optionType;
                signedStrike_ = (int)optionType * (strike + displacement);
                signedForward_ = (int)optionType * (forward + displacement);
                undiscountedBlackPrice_ = undiscountedBlackPrice;
                N_ = new CumulativeNormalDistribution();
                checkParameters(strike, forward, displacement);
                QLNet.Utils.QL_REQUIRE(undiscountedBlackPrice >= 0.0, () =>
                    "undiscounted Black price (" +
                    undiscountedBlackPrice + ") must be non-negative");
                signedMoneyness_ = (int)optionType * System.Math.Log((forward + displacement) / (strike + displacement));
            }

            public override double derivative(double stdDev)
            {
#if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(stdDev >= 0.0,
                       "stdDev (" << stdDev << ") must be non-negative");
#endif
                var signedD1 = signedMoneyness_ / stdDev + halfOptionType_ * stdDev;
                return signedForward_ * N_.derivative(signedD1);
            }

            public override double value(double stdDev)
            {
#if QL_EXTRA_SAFETY_CHECKS
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "stdDev (" + stdDev + ") must be non-negative");
#endif
                if (stdDev.IsEqual(0.0))
                {
                    return System.Math.Max(signedForward_ - signedStrike_, 0.0)
                           - undiscountedBlackPrice_;
                }

                var temp = halfOptionType_ * stdDev;
                var d = signedMoneyness_ / stdDev;
                var signedD1 = d + temp;
                var signedD2 = d - temp;
                var result = signedForward_ * N_.value(signedD1)
                             - signedStrike_ * N_.value(signedD2);
                // numerical inaccuracies can yield a negative answer
                return System.Math.Max(0.0, result) - undiscountedBlackPrice_;
            }
        }

        /*! Black style formula when forward is normal rather than
           log-normal. This is essentially the model of Bachelier.
  
            \warning Bachelier model needs absolute volatility, not
               percentage volatility. Standard deviation is
               absoluteVolatility*sqrt(timeToMaturity)
        */
        public static double bachelierBlackFormula(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double stdDev,
            double discount = 1.0)
        {
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () =>
                "stdDev (" + stdDev + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");
            double d = (forward - strike) * (int)optionType, h = d / stdDev;
            if (stdDev.IsEqual(0.0))
            {
                return discount * System.Math.Max(d, 0.0);
            }

            var phi = new CumulativeNormalDistribution();
            var result = discount * (stdDev * phi.derivative(h) + d * phi.value(h));
            QLNet.Utils.QL_REQUIRE(result >= 0.0, () =>
                "negative value (" + result + ") for " +
                stdDev + " stdDev, " +
                optionType + " option, " +
                strike + " strike , " +
                forward + " forward");
            return result;
        }

        public static double bachelierBlackFormula(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double discount = 1.0) =>
            bachelierBlackFormula(payoff.optionType(), payoff.strike(), forward, stdDev, discount);

        /*! Approximated Bachelier implied volatility
  
           It is calculated using  the analytic implied volatility approximation
           of J. Choi, K Kim and M. Kwak (2009), “Numerical Approximation of the
           Implied Volatility Under Arithmetic Brownian Motion”,
           Applied Math. Finance, 16(3), pp. 261-268.
        */
        public static double bachelierBlackFormulaImpliedVol(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double tte,
            double bachelierPrice,
            double discount = 1.0)
        {
            var SQRT_QL_EPSILON = System.Math.Sqrt(Const.QL_EPSILON);

            QLNet.Utils.QL_REQUIRE(tte > 0.0, () => "tte (" + tte + ") must be positive");

            var forwardPremium = bachelierPrice / discount;

            double straddlePremium;
            if (optionType == QLNet.Option.Type.Call)
            {
                straddlePremium = 2.0 * forwardPremium - (forward - strike);
            }
            else
            {
                straddlePremium = 2.0 * forwardPremium + (forward - strike);
            }

            var nu = (forward - strike) / straddlePremium;
            QLNet.Utils.QL_REQUIRE(nu <= 1.0, () => "nu (" + nu + ") must be <= 1.0");
            QLNet.Utils.QL_REQUIRE(nu >= -1.0, () => "nu (" + nu + ") must be >= -1.0");

            nu = System.Math.Max(-1.0 + Const.QL_EPSILON, System.Math.Min(nu, 1.0 - Const.QL_EPSILON));

            // nu / arctanh(nu) -> 1 as nu -> 0
            var eta = (System.Math.Abs(nu) < SQRT_QL_EPSILON) ? 1.0 : nu / ((System.Math.Log(1 + nu) - System.Math.Log(1 - nu)) / 2);

            var heta = h(eta);

            var impliedBpvol = System.Math.Sqrt(Const.M_PI / (2 * tte)) * straddlePremium * heta;

            return impliedBpvol;
        }

        /*! Bachelier formula for standard deviation derivative
              \warning instead of volatility it uses standard deviation, i.e.
               volatility*sqrt(timeToMaturity), and it returns the
               derivative with respect to the standard deviation.
               If T is the time to maturity Black vega would be
               blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
        */

        public static double bachelierBlackFormulaStdDevDerivative(double strike,
            double forward,
            double stdDev,
            double discount = 1.0)
        {
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () =>
                "stdDev (" + stdDev + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            if (stdDev.IsEqual(0.0))
            {
                return 0.0;
            }

            var d1 = (forward - strike) / stdDev;
            return discount *
                   new CumulativeNormalDistribution().derivative(d1);
        }

        public static double bachelierBlackFormulaStdDevDerivative(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double discount = 1.0) =>
            bachelierBlackFormulaStdDevDerivative(payoff.strike(), forward, stdDev, discount);

        /*! Black 1976 formula
          \warning instead of volatility it uses standard deviation,
                   i.e. volatility*sqrt(timeToMaturity)
        */
        public static double blackFormula(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double stdDev,
            double discount = 1.0,
            double displacement = 0.0)
        {
            checkParameters(strike, forward, displacement);
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "stdDev (" + stdDev + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () => "discount (" + discount + ") must be positive");

            if (stdDev.IsEqual(0.0))
            {
                return System.Math.Max((forward - strike) * (int)optionType, 0.0) * discount;
            }

            forward = forward + displacement;
            strike = strike + displacement;

            // since displacement is non-negative strike==0 iff displacement==0
            // so returning forward*discount is OK
            if (strike.IsEqual(0.0))
            {
                return (optionType == QLNet.Option.Type.Call ? forward * discount : 0.0);
            }

            var d1 = System.Math.Log(forward / strike) / stdDev + 0.5 * stdDev;
            var d2 = d1 - stdDev;
            var phi = new CumulativeNormalDistribution();
            var nd1 = phi.value((int)optionType * d1);
            var nd2 = phi.value((int)optionType * d2);
            var result = discount * (int)optionType * (forward * nd1 - strike * nd2);
            QLNet.Utils.QL_REQUIRE(result >= 0.0, () =>
                "negative value (" + result + ") for " +
                stdDev + " stdDev, " +
                optionType + " option, " +
                strike + " strike , " +
                forward + " forward");
            return result;
        }

        public static double blackFormula(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double discount = 1.0,
            double displacement = 0.0) =>
            blackFormula(payoff.optionType(), payoff.strike(), forward, stdDev, discount, displacement);

        /*! Black 1976 probability of being in the money (in the bond martingale measure), i.e. N(d2).
              It is a risk-neutral probability, not the real world one.
               \warning instead of volatility it uses standard deviation, i.e. volatility*sqrt(timeToMaturity)
        */
        public static double blackFormulaCashItmProbability(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double stdDev,
            double displacement = 0.0)
        {
            checkParameters(strike, forward, displacement);
            if (stdDev.IsEqual(0.0))
            {
                return (forward * (int)optionType > strike * (int)optionType ? 1.0 : 0.0);
            }

            forward = forward + displacement;
            strike = strike + displacement;
            if (strike.IsEqual(0.0))
            {
                return (optionType == QLNet.Option.Type.Call ? 1.0 : 0.0);
            }

            var d2 = System.Math.Log(forward / strike) / stdDev - 0.5 * stdDev;
            var phi = new CumulativeNormalDistribution();
            return phi.value((int)optionType * d2);
        }

        public static double blackFormulaCashItmProbability(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double displacement = 0.0) =>
            blackFormulaCashItmProbability(payoff.optionType(),
                payoff.strike(), forward, stdDev, displacement);

        /*! Black 1976 implied standard deviation,
              i.e. volatility*sqrt(timeToMaturity)
        */
        public static double blackFormulaImpliedStdDev(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double blackPrice,
            double discount = 1.0,
            double displacement = 0.0,
            double? guess = null,
            double accuracy = 1.0e-6,
            int maxIterations = 100)
        {
            checkParameters(strike, forward, displacement);

            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            QLNet.Utils.QL_REQUIRE(blackPrice >= 0.0, () =>
                "option price (" + blackPrice + ") must be non-negative");
            // check the price of the "other" option implied by put-call paity
            var otherOptionPrice = blackPrice - (int)optionType * (forward - strike) * discount;
            QLNet.Utils.QL_REQUIRE(otherOptionPrice >= 0.0, () =>
                "negative " + (-1 * (int)optionType) +
                " price (" + otherOptionPrice +
                ") implied by put-call parity. No solution exists for " +
                optionType + " strike " + strike +
                ", forward " + forward +
                ", price " + blackPrice +
                ", deflator " + discount);

            // solve for the out-of-the-money option which has
            // greater vega/price ratio, i.e.
            // it is numerically more robust for implied vol calculations
            if (optionType == QLNet.Option.Type.Put && strike > forward)
            {
                optionType = QLNet.Option.Type.Call;
                blackPrice = otherOptionPrice;
            }

            if (optionType == QLNet.Option.Type.Call && strike < forward)
            {
                optionType = QLNet.Option.Type.Put;
                blackPrice = otherOptionPrice;
            }

            strike = strike + displacement;
            forward = forward + displacement;

            if (guess == null)
            {
                guess = blackFormulaImpliedStdDevApproximation(optionType, strike, forward, blackPrice, discount, displacement);
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(guess >= 0.0, () => "stdDev guess (" + guess + ") must be non-negative");
            }

            var f = new BlackImpliedStdDevHelper(optionType, strike, forward, blackPrice / discount);
            var solver = new NewtonSafe();
            solver.setMaxEvaluations(maxIterations);
            double minSdtDev = 0.0, maxStdDev = 24.0; // 24 = 300% * sqrt(60)
            var stdDev = solver.solve(f, accuracy, guess.Value, minSdtDev, maxStdDev);
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "stdDev (" + stdDev + ") must be non-negative");
            return stdDev;
        }

        public static double blackFormulaImpliedStdDev(PlainVanillaPayoff payoff,
            double forward,
            double blackPrice,
            double discount,
            double displacement,
            double guess,
            double accuracy,
            int maxIterations = 100) =>
            blackFormulaImpliedStdDev(payoff.optionType(), payoff.strike(),
                forward, blackPrice, discount, displacement, guess, accuracy, maxIterations);

        /*! Approximated Black 1976 implied standard deviation,
            i.e. volatility*sqrt(timeToMaturity).
  
            It is calculated using Brenner and Subrahmanyan (1988) and Feinstein
            (1988) approximation for at-the-money forward option, with the
            extended moneyness approximation by Corrado and Miller (1996)
        */
        public static double blackFormulaImpliedStdDevApproximation(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double blackPrice,
            double discount = 1.0,
            double displacement = 0.0)
        {
            checkParameters(strike, forward, displacement);
            QLNet.Utils.QL_REQUIRE(blackPrice >= 0.0, () =>
                "blackPrice (" + blackPrice + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            double stdDev;
            forward = forward + displacement;
            strike = strike + displacement;
            if (strike.IsEqual(forward))
                // Brenner-Subrahmanyan (1988) and Feinstein (1988) ATM approx.
            {
                stdDev = blackPrice / discount * System.Math.Sqrt(2.0 * Const.M_PI) / forward;
            }
            else
            {
                // Corrado and Miller extended moneyness approximation
                var moneynessDelta = (int)optionType * (forward - strike);
                var moneynessDelta_2 = moneynessDelta / 2.0;
                var temp = blackPrice / discount - moneynessDelta_2;
                var moneynessDelta_PI = moneynessDelta * moneynessDelta / Const.M_PI;
                var temp2 = temp * temp - moneynessDelta_PI;
                if (temp2 < 0.0) // approximation breaks down, 2 alternatives:
                    // 1. zero it
                {
                    temp2 = 0.0;
                }

                // 2. Manaster-Koehler (1982) efficient Newton-Raphson seed
                temp2 = System.Math.Sqrt(temp2);
                temp += temp2;
                temp *= System.Math.Sqrt(2.0 * Const.M_PI);
                stdDev = temp / (forward + strike);
            }

            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "stdDev (" + stdDev + ") must be non-negative");
            return stdDev;
        }

        public static double blackFormulaImpliedStdDevApproximation(PlainVanillaPayoff payoff,
            double forward,
            double blackPrice,
            double discount,
            double displacement) =>
            blackFormulaImpliedStdDevApproximation(payoff.optionType(),
                payoff.strike(), forward, blackPrice, discount, displacement);

        /*! Approximated Black 1976 implied standard deviation,
            i.e. volatility*sqrt(timeToMaturity).
  
            It is calculated following "An improved approach to computing
            implied volatility", Chambers, Nawalkha, The Financial Review,
            2001, 89-100. The atm option price must be known to use this
            method.
        */
        public static double blackFormulaImpliedStdDevChambers(QLNet.Option.Type optionType,
            double strike,
            double forward,
            double blackPrice,
            double blackAtmPrice,
            double discount = 1.0,
            double displacement = 0.0)
        {
            checkParameters(strike, forward, displacement);
            QLNet.Utils.QL_REQUIRE(blackPrice >= 0.0, () =>
                "blackPrice (" + blackPrice + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(blackAtmPrice >= 0.0, () =>
                "blackAtmPrice (" + blackAtmPrice + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            double stdDev;

            forward = forward + displacement;
            strike = strike + displacement;
            blackPrice /= discount;
            blackAtmPrice /= discount;

            var s0 = Const.M_SQRT2 * Const.M_SQRTPI * blackAtmPrice /
                     forward; // Brenner-Subrahmanyam formula
            var priceAtmVol = blackFormula(optionType, strike, forward, s0);
            var dc = blackPrice - priceAtmVol;

            if (Math.Utils.close(dc, 0.0))
            {
                stdDev = s0;
            }
            else
            {
                var d1 = blackFormulaStdDevDerivative(strike, forward, s0);
                var d2 = blackFormulaStdDevSecondDerivative(strike, forward, s0, 1.0, 0.0);
                var ds = 0.0;
                var tmp = d1 * d1 + 2.0 * d2 * dc;
                if (System.Math.Abs(d2) > 1E-10 && tmp >= 0.0)
                {
                    ds = (-d1 + System.Math.Sqrt(tmp)) / d2; // second order approximation
                }
                else if (System.Math.Abs(d1) > 1E-10)
                {
                    ds = dc / d1; // first order approximation
                }

                stdDev = s0 + ds;
            }

            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () => "stdDev (" + stdDev + ") must be non-negative");
            return stdDev;
        }

        public static double blackFormulaImpliedStdDevChambers(PlainVanillaPayoff payoff,
            double forward,
            double blackPrice,
            double blackAtmPrice,
            double discount,
            double displacement) =>
            blackFormulaImpliedStdDevChambers(payoff.optionType(), payoff.strike(), forward, blackPrice,
                blackAtmPrice, discount, displacement);

        /*! Black 1976 formula for standard deviation derivative
            \warning instead of volatility it uses standard deviation, i.e.
                     volatility*sqrt(timeToMaturity), and it returns the
                     derivative with respect to the standard deviation.
                     If T is the time to maturity Black vega would be
                     blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
        */
        public static double blackFormulaStdDevDerivative(double strike,
            double forward,
            double stdDev,
            double discount = 1.0,
            double displacement = 0.0)
        {
            checkParameters(strike, forward, displacement);
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () =>
                "stdDev (" + stdDev + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            forward = forward + displacement;
            strike = strike + displacement;

            if (stdDev.IsEqual(0.0) || strike.IsEqual(0.0))
            {
                return 0.0;
            }

            var d1 = System.Math.Log(forward / strike) / stdDev + .5 * stdDev;
            return discount * forward *
                   new CumulativeNormalDistribution().derivative(d1);
        }

        public static double blackFormulaStdDevDerivative(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double discount = 1.0,
            double displacement = 0.0) =>
            blackFormulaStdDevDerivative(payoff.strike(), forward, stdDev, discount, displacement);

        /*! Black 1976 formula for second derivative by standard deviation
              \warning instead of volatility it uses standard deviation, i.e.
               volatility*sqrt(timeToMaturity), and it returns the
               derivative with respect to the standard deviation.
        */
        public static double blackFormulaStdDevSecondDerivative(double strike,
            double forward,
            double stdDev,
            double discount,
            double displacement)
        {
            checkParameters(strike, forward, displacement);
            QLNet.Utils.QL_REQUIRE(stdDev >= 0.0, () =>
                "stdDev (" + stdDev + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () =>
                "discount (" + discount + ") must be positive");

            forward = forward + displacement;
            strike = strike + displacement;

            if (stdDev.IsEqual(0.0) || strike.IsEqual(0.0))
            {
                return 0.0;
            }

            var d1 = System.Math.Log(forward / strike) / stdDev + .5 * stdDev;
            var d1p = -System.Math.Log(forward / strike) / (stdDev * stdDev) + .5;
            return discount * forward *
                   new NormalDistribution().derivative(d1) * d1p;
        }

        public static double blackFormulaStdDevSecondDerivative(PlainVanillaPayoff payoff,
            double forward,
            double stdDev,
            double discount = 1.0,
            double displacement = 0.0) =>
            blackFormulaStdDevSecondDerivative(payoff.strike(), forward, stdDev, discount, displacement);

        /*! Black 1976 formula for  derivative with respect to implied vol, this
          is basically the vega, but if you want 1% change multiply by 1%
        */
        public static double blackFormulaVolDerivative(double strike,
            double forward,
            double stdDev,
            double expiry,
            double discount = 1.0,
            double displacement = 0.0) =>
            blackFormulaStdDevDerivative(strike, forward, stdDev, discount, displacement) * System.Math.Sqrt(expiry);

        public static void checkParameters(double strike, double forward, double displacement)
        {
            QLNet.Utils.QL_REQUIRE(displacement >= 0.0, () =>
                "displacement (" + displacement + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(strike + displacement >= 0.0, () =>
                "strike + displacement (" + strike + " + " + displacement + ") must be non-negative");
            QLNet.Utils.QL_REQUIRE(forward + displacement > 0.0, () =>
                "forward + displacement (" + forward + " + " + displacement + ") must be positive");
        }

        public static double h(double eta)
        {
            const double A0 = 3.994961687345134e-1;
            const double A1 = 2.100960795068497e+1;
            const double A2 = 4.980340217855084e+1;
            const double A3 = 5.988761102690991e+2;
            const double A4 = 1.848489695437094e+3;
            const double A5 = 6.106322407867059e+3;
            const double A6 = 2.493415285349361e+4;
            const double A7 = 1.266458051348246e+4;

            const double B0 = 1.000000000000000e+0;
            const double B1 = 4.990534153589422e+1;
            const double B2 = 3.093573936743112e+1;
            const double B3 = 1.495105008310999e+3;
            const double B4 = 1.323614537899738e+3;
            const double B5 = 1.598919697679745e+4;
            const double B6 = 2.392008891720782e+4;
            const double B7 = 3.608817108375034e+3;
            const double B8 = -2.067719486400926e+2;
            const double B9 = 1.174240599306013e+1;

            QLNet.Utils.QL_REQUIRE(eta >= 0.0, () =>
                "eta (" + eta + ") must be non-negative");

            var num = A0 + eta * (A1 + eta * (A2 + eta * (A3 + eta * (A4 + eta
                * (A5 + eta * (A6 + eta * A7))))));

            var den = B0 + eta * (B1 + eta * (B2 + eta * (B3 + eta * (B4 + eta
                * (B5 + eta * (B6 + eta * (B7 + eta * (B8 + eta * B9))))))));

            return System.Math.Sqrt(eta) * (num / den);
        }
    }
}
