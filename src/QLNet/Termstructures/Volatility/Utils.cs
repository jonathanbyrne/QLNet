/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Termstructures.Volatility
{
    public partial class Utils
    {
        public static double sabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
            double nu, double rho)
        {
            QLNet.Utils.QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrNormalVolatility(strike, forward, expiryTime, alpha, beta, nu, rho);
        }

        public static double sabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
            double nu, double rho, SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            QLNet.Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive: " + strike + " not allowed");
            QLNet.Utils.QL_REQUIRE(forward > 0.0, () => "at the money forward rate must be: " + forward + " not allowed");
            QLNet.Utils.QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrVolatility(strike, forward, expiryTime, alpha, beta, nu, rho, approximationModel);
        }

        public static double shiftedSabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
            double nu, double rho, double shift = 0.0)
        {
            QLNet.Utils.QL_REQUIRE(strike + shift > 0.0, () => "strike+shift must be positive: "
                                                               + strike + "+" + shift + " not allowed");
            QLNet.Utils.QL_REQUIRE(forward + shift > 0.0, () => "at the money forward rate + shift must be "
                                                                + "positive: " + forward + " " + shift + " not allowed");
            QLNet.Utils.QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrNormalVolatility(strike + shift, forward + shift, expiryTime, alpha, beta, nu, rho);
        }

        public static double shiftedSabrVolatility(double strike,
            double forward,
            double expiryTime,
            double alpha,
            double beta,
            double nu,
            double rho,
            double shift,
            SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            QLNet.Utils.QL_REQUIRE(strike + shift > 0.0, () => "strike+shift must be positive: "
                                                               + strike + "+" + shift + " not allowed");
            QLNet.Utils.QL_REQUIRE(forward + shift > 0.0, () => "at the money forward rate + shift must be "
                                                                + "positive: " + forward + " " + shift + " not allowed");
            QLNet.Utils.QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: "
                                                            + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeShiftedSabrVolatility(strike, forward, expiryTime,
                alpha, beta, nu, rho, shift, approximationModel);
        }

        public static double unsafeSabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
            double nu, double rho)
        {
            var oneMinusBeta = 1.0 - beta;
            var Fmid = forward * strike < 0.0 ? (forward + strike) * 0.5 : System.Math.Sqrt(forward * strike);
            var gamma1 = beta / Fmid;
            var gamma2 = -beta * oneMinusBeta / (Fmid * Fmid);
            var zeta = alpha / (nu * oneMinusBeta) * (System.Math.Pow(forward, oneMinusBeta) - System.Math.Pow(strike, oneMinusBeta));
            var D = System.Math.Log((System.Math.Sqrt(1.0 - 2.0 * rho * zeta + zeta * zeta) + zeta - rho) / (1.0 - rho));
            var epsilon = alpha * alpha * expiryTime;
            var M = forward - strike;
            var a = nu * System.Math.Pow(Fmid, beta) / alpha;
            var b = System.Math.Pow(a, 2.0);
            var d = 1.0 + ((2.0 * gamma2 - gamma1 * gamma1) / 24.0 * b
                           + rho * gamma1 / 4.0 * a
                           + (2.0 - 3.0 * rho * rho) / 24.0) * epsilon;

            return alpha * M / D * d;
        }

        public static double unsafeSabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
            double nu, double rho, SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            if (approximationModel == SabrApproximationModel.Hagan2002)
            {
                var oneMinusBeta = 1.0 - beta;
                var A = System.Math.Pow(forward * strike, oneMinusBeta);
                var sqrtA = System.Math.Sqrt(A);
                double logM;

                if (!Math.Utils.close(forward, strike))
                {
                    logM = System.Math.Log(forward / strike);
                }
                else
                {
                    var epsilon = (forward - strike) / strike;
                    logM = epsilon - .5 * epsilon * epsilon;
                }

                var z = (nu / alpha) * sqrtA * logM;
                var B = 1.0 - 2.0 * rho * z + z * z;
                var C = oneMinusBeta * oneMinusBeta * logM * logM;
                var tmp = (System.Math.Sqrt(B) + z - rho) / (1.0 - rho);
                var xx = System.Math.Log(tmp);
                var D = sqrtA * (1.0 + C / 24.0 + C * C / 1920.0);
                var d = 1.0 + expiryTime *
                    (oneMinusBeta * oneMinusBeta * alpha * alpha / (24.0 * A)
                     + 0.25 * rho * beta * nu * alpha / sqrtA
                     + (2.0 - 3.0 * rho * rho) * (nu * nu / 24.0));

                double multiplier;
                // computations become precise enough if the square of z worth slightly more than the precision machine (hence the m)
                const double m = 10;

                if (System.Math.Abs(z * z) > Const.QL_EPSILON * m)
                {
                    multiplier = z / xx;
                }
                else
                {
                    multiplier = 1.0 - 0.5 * rho * z - (3.0 * rho * rho - 2.0) * z * z / 12.0;
                }

                return (alpha / D) * multiplier * d;
            }

            if (approximationModel == SabrApproximationModel.Obloj2008)
            {
                var oneMinusBeta = 1.0 - beta;
                var Fmid = System.Math.Sqrt(forward * strike);
                var gamma1 = beta / Fmid;
                var gamma2 = -beta * oneMinusBeta / (Fmid * Fmid);
                var zeta = alpha / (nu * oneMinusBeta) * (System.Math.Pow(forward, oneMinusBeta) - System.Math.Pow(strike, oneMinusBeta));
                var D = System.Math.Log((System.Math.Sqrt(1.0 - 2.0 * rho * zeta + zeta * zeta) + zeta - rho) / (1.0 - rho));
                var epsilon = alpha * alpha * expiryTime;

                double logM;

                if (!Math.Utils.close(forward, strike))
                {
                    logM = System.Math.Log(forward / strike);
                }
                else
                {
                    var eps = (forward - strike) / strike;
                    logM = eps - .5 * eps * eps;
                }

                var a = nu * System.Math.Pow(Fmid, beta) / alpha;
                var b = System.Math.Pow(a, 2.0);
                var d = 1.0 + ((2.0 * gamma2 - gamma1 * gamma1 + 1 / (Fmid * Fmid)) / 24.0 * b
                               + rho * gamma1 / 4.0 * a
                               + (2.0 - 3.0 * rho * rho) / 24.0) * epsilon;

                return alpha * logM / D * d;
            }

            QLNet.Utils.QL_FAIL("Unknown approximation model.");
            return 0.0;
        }

        public static double unsafeShiftedSabrVolatility(double strike,
            double forward,
            double expiryTime,
            double alpha,
            double beta,
            double nu,
            double rho,
            double shift,
            SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002) =>
            unsafeSabrVolatility(strike + shift, forward + shift, expiryTime,
                alpha, beta, nu, rho, approximationModel);

        public static void validateSabrParameters(double alpha, double beta, double nu, double rho)
        {
            QLNet.Utils.QL_REQUIRE(alpha > 0.0, () => "alpha must be positive: " + alpha + " not allowed");
            QLNet.Utils.QL_REQUIRE(beta >= 0.0 && beta <= 1.0, () => "beta must be in (0.0, 1.0): " + beta + " not allowed");
            QLNet.Utils.QL_REQUIRE(nu >= 0.0, () => "nu must be non negative: " + nu + " not allowed");
            QLNet.Utils.QL_REQUIRE(rho * rho < 1.0, () => "rho square must be less than one: " + rho + " not allowed");
        }
    }
}
