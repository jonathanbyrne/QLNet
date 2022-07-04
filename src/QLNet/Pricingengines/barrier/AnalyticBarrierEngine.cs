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
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.processes;
using System;

namespace QLNet.Pricingengines.barrier
{

    //! Pricing Engine for barrier options using analytical formulae
    //    ! The formulas are taken from "Option pricing formulas",
    //         E.G. Haug, McGraw-Hill, p.69 and following.
    //
    //        \ingroup barrierengines
    //
    //        \test the correctness of the returned value is tested by
    //              reproducing results available in literature.
    //
    [JetBrains.Annotations.PublicAPI] public class AnalyticBarrierEngine : BarrierOption.Engine
    {
        public AnalyticBarrierEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }
        public override void calculate()
        {

            var payoff = arguments_.payoff as PlainVanillaPayoff;

            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            Utils.QL_REQUIRE(payoff.strike() > 0.0, () => "strike must be positive");

            var strike = payoff.strike();
            var spot = process_.x0();

            Utils.QL_REQUIRE(spot >= 0.0, () => "negative or null underlying given");
            Utils.QL_REQUIRE(!triggered(spot), () => "barrier touched");

            var barrierType = arguments_.barrierType;

            switch (payoff.optionType())
            {
                case QLNet.Option.Type.Call:
                    switch (barrierType)
                    {
                        case Barrier.Type.DownIn:
                            if (strike >= barrier())
                                results_.value = C(1, 1) + E(1);
                            else
                                results_.value = A(1) - B(1) + D(1, 1) + E(1);
                            break;
                        case Barrier.Type.UpIn:
                            if (strike >= barrier())
                                results_.value = A(1) + E(-1);
                            else
                                results_.value = B(1) - C(-1, 1) + D(-1, 1) + E(-1);
                            break;
                        case Barrier.Type.DownOut:
                            if (strike >= barrier())
                                results_.value = A(1) - C(1, 1) + F(1);
                            else
                                results_.value = B(1) - D(1, 1) + F(1);
                            break;
                        case Barrier.Type.UpOut:
                            if (strike >= barrier())
                                results_.value = F(-1);
                            else
                                results_.value = A(1) - B(1) + C(-1, 1) - D(-1, 1) + F(-1);
                            break;
                    }
                    break;
                case QLNet.Option.Type.Put:
                    switch (barrierType)
                    {
                        case Barrier.Type.DownIn:
                            if (strike >= barrier())
                                results_.value = B(-1) - C(1, -1) + D(1, -1) + E(1);
                            else
                                results_.value = A(-1) + E(1);
                            break;
                        case Barrier.Type.UpIn:
                            if (strike >= barrier())
                                results_.value = A(-1) - B(-1) + D(-1, -1) + E(-1);
                            else
                                results_.value = C(-1, -1) + E(-1);
                            break;
                        case Barrier.Type.DownOut:
                            if (strike >= barrier())
                                results_.value = A(-1) - B(-1) + C(1, -1) - D(1, -1) + F(1);
                            else
                                results_.value = F(1);
                            break;
                        case Barrier.Type.UpOut:
                            if (strike >= barrier())
                                results_.value = B(-1) - D(-1, -1) + F(-1);
                            else
                                results_.value = A(-1) - C(-1, -1) + F(-1);
                            break;
                    }
                    break;
                default:
                    Utils.QL_FAIL("unknown ExerciseType");
                    break;
            }
        }
        private GeneralizedBlackScholesProcess process_;
        private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();

        private double underlying() => process_.x0();

        private double strike()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            return payoff.strike();
        }
        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), strike());

        private double barrier() => arguments_.barrier.GetValueOrDefault();

        private double rebate() => arguments_.rebate.GetValueOrDefault();

        private double stdDeviation() => volatility() * System.Math.Sqrt(residualTime());

        private double riskFreeRate() => process_.riskFreeRate().link.zeroRate(residualTime(), Compounding.Continuous, Frequency.NoFrequency).rate();

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double dividendYield() => process_.dividendYield().link.zeroRate(residualTime(), Compounding.Continuous, Frequency.NoFrequency).rate();

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double mu()
        {
            var vol = volatility();
            return (riskFreeRate() - dividendYield()) / (vol * vol) - 0.5;
        }
        private double muSigma() => (1 + mu()) * stdDeviation();

        private double A(double phi)
        {
            var x1 = System.Math.Log(underlying() / strike()) / stdDeviation() + muSigma();
            var N1 = f_.value(phi * x1);
            var N2 = f_.value(phi * (x1 - stdDeviation()));
            return phi * (underlying() * dividendDiscount() * N1 - strike() * riskFreeDiscount() * N2);
        }
        private double B(double phi)
        {
            var x2 = System.Math.Log(underlying() / barrier()) / stdDeviation() + muSigma();
            var N1 = f_.value(phi * x2);
            var N2 = f_.value(phi * (x2 - stdDeviation()));
            return phi * (underlying() * dividendDiscount() * N1 - strike() * riskFreeDiscount() * N2);
        }
        private double C(double eta, double phi)
        {
            var HS = barrier() / underlying();
            var powHS0 = System.Math.Pow(HS, 2 * mu());
            var powHS1 = powHS0 * HS * HS;
            var y1 = System.Math.Log(barrier() * HS / strike()) / stdDeviation() + muSigma();
            var N1 = f_.value(eta * y1);
            var N2 = f_.value(eta * (y1 - stdDeviation()));
            return phi * (underlying() * dividendDiscount() * powHS1 * N1 - strike() * riskFreeDiscount() * powHS0 * N2);
        }
        private double D(double eta, double phi)
        {
            var HS = barrier() / underlying();
            var powHS0 = System.Math.Pow(HS, 2 * mu());
            var powHS1 = powHS0 * HS * HS;
            var y2 = System.Math.Log(barrier() / underlying()) / stdDeviation() + muSigma();
            var N1 = f_.value(eta * y2);
            var N2 = f_.value(eta * (y2 - stdDeviation()));
            return phi * (underlying() * dividendDiscount() * powHS1 * N1 - strike() * riskFreeDiscount() * powHS0 * N2);
        }
        private double E(double eta)
        {
            if (rebate() > 0)
            {
                var powHS0 = System.Math.Pow(barrier() / underlying(), 2 * mu());
                var x2 = System.Math.Log(underlying() / barrier()) / stdDeviation() + muSigma();
                var y2 = System.Math.Log(barrier() / underlying()) / stdDeviation() + muSigma();
                var N1 = f_.value(eta * (x2 - stdDeviation()));
                var N2 = f_.value(eta * (y2 - stdDeviation()));
                return rebate() * riskFreeDiscount() * (N1 - powHS0 * N2);
            }
            else
            {
                return 0.0;
            }
        }
        private double F(double eta)
        {
            if (rebate() > 0)
            {
                var m = mu();
                var vol = volatility();
                var lambda = System.Math.Sqrt(m * m + 2.0 * riskFreeRate() / (vol * vol));
                var HS = barrier() / underlying();
                var powHSplus = System.Math.Pow(HS, m + lambda);
                var powHSminus = System.Math.Pow(HS, m - lambda);

                var sigmaSqrtT = stdDeviation();
                var z = System.Math.Log(barrier() / underlying()) / sigmaSqrtT + lambda * sigmaSqrtT;

                var N1 = f_.value(eta * z);
                var N2 = f_.value(eta * (z - 2.0 * lambda * sigmaSqrtT));
                return rebate() * (powHSplus * N1 + powHSminus * N2);
            }
            else
            {
                return 0.0;
            }
        }
    }
}