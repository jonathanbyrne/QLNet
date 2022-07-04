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
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Pricingengines;
using QLNet.processes;
using System;

namespace QLNet.Pricingengines.barrier
{
    //! Pricing engine for double barrier european options using analytical formulae
    /*! The formulas are taken from "The complete guide to option pricing formulas 2nd Ed",
         E.G. Haug, McGraw-Hill, p.156 and following.
         Implements the Ikeda and Kunitomo series (see "Pricing Options with
         Curved Boundaries" Mathematical Finance 2/1992").
         This code handles only flat barriers

        \ingroup barrierengines

        \note the formula holds only when strike is in the barrier range

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    [JetBrains.Annotations.PublicAPI] public class AnalyticDoubleBarrierEngine : DoubleBarrierOption.Engine
    {
        public AnalyticDoubleBarrierEngine(GeneralizedBlackScholesProcess process, int series = 5)
        {
            process_ = process;
            series_ = series;
            f_ = new CumulativeNormalDistribution();

            process_.registerWith(update);
        }
        public override void calculate()
        {
            Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () =>
                             "this engine handles only european options");

            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

            var strike = payoff.strike();
            Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");

            var spot = underlying();
            Utils.QL_REQUIRE(spot >= 0.0, () => "negative or null underlying given");
            Utils.QL_REQUIRE(!triggered(spot), () => "barrier(s) already touched");

            var barrierType = arguments_.barrierType;

            if (triggered(spot))
            {
                if (barrierType == DoubleBarrier.Type.KnockIn)
                    results_.value = vanillaEquivalent();  // knocked in
                else
                    results_.value = 0.0;  // knocked out
            }
            else
            {
                switch (payoff.optionType())
                {
                    case QLNet.Option.Type.Call:
                        switch (barrierType)
                        {
                            case DoubleBarrier.Type.KnockIn:
                                results_.value = callKI();
                                break;
                            case DoubleBarrier.Type.KnockOut:
                                results_.value = callKO();
                                break;
                            case DoubleBarrier.Type.KIKO:
                            case DoubleBarrier.Type.KOKI:
                                Utils.QL_FAIL("unsupported double-barrier ExerciseType: " + barrierType);
                                break;
                            default:
                                Utils.QL_FAIL("unknown double-barrier ExerciseType: " + barrierType);
                                break;
                        }
                        break;
                    case QLNet.Option.Type.Put:
                        switch (barrierType)
                        {
                            case DoubleBarrier.Type.KnockIn:
                                results_.value = putKI();
                                break;
                            case DoubleBarrier.Type.KnockOut:
                                results_.value = putKO();
                                break;
                            case DoubleBarrier.Type.KIKO:
                            case DoubleBarrier.Type.KOKI:
                                Utils.QL_FAIL("unsupported double-barrier ExerciseType: " + barrierType);
                                break;
                            default:
                                Utils.QL_FAIL("unknown double-barrier ExerciseType: " + barrierType);
                                break;
                        }
                        break;
                    default:
                        Utils.QL_FAIL("unknown ExerciseType");
                        break;
                }
            }
        }

        private GeneralizedBlackScholesProcess process_;
        private CumulativeNormalDistribution f_;
        private int series_;
        // helper methods
        private double underlying() => process_.x0();

        private double strike()
        {
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");
            return payoff.strike();
        }
        private double residualTime() => process_.time(arguments_.exercise.lastDate());

        private double volatility() => process_.blackVolatility().link.blackVol(residualTime(), strike());

        private double volatilitySquared() => volatility() * volatility();

        private double barrierLo() => arguments_.barrier_lo.GetValueOrDefault();

        private double barrierHi() => arguments_.barrier_hi.GetValueOrDefault();

        private double rebate() => arguments_.rebate.GetValueOrDefault();

        private double stdDeviation() => volatility() * System.Math.Sqrt(residualTime());

        private double riskFreeRate() =>
            process_.riskFreeRate().link.zeroRate(
                residualTime(), Compounding.Continuous, Frequency.NoFrequency).value();

        private double riskFreeDiscount() => process_.riskFreeRate().link.discount(residualTime());

        private double dividendYield() =>
            process_.dividendYield().link.zeroRate(
                residualTime(), Compounding.Continuous, Frequency.NoFrequency).value();

        private double costOfCarry() => riskFreeRate() - dividendYield();

        private double dividendDiscount() => process_.dividendYield().link.discount(residualTime());

        private double vanillaEquivalent()
        {
            // Call KI equates to vanilla - callKO
            var payoff = arguments_.payoff as StrikedTypePayoff;
            var forwardPrice = underlying() * dividendDiscount() / riskFreeDiscount();
            var black = new BlackCalculator(payoff, forwardPrice, stdDeviation(), riskFreeDiscount());
            var vanilla = black.value();
            if (vanilla < 0.0)
                vanilla = 0.0;
            return vanilla;
        }
        private double callKO()
        {
            // N.B. for flat barriers mu3=mu1 and mu2=0
            var mu1 = 2 * costOfCarry() / volatilitySquared() + 1;
            var bsigma = (costOfCarry() + volatilitySquared() / 2.0) * residualTime() / stdDeviation();

            double acc1 = 0;
            double acc2 = 0;
            for (var n = -series_; n <= series_; ++n)
            {
                var L2n = System.Math.Pow(barrierLo(), 2 * n);
                var U2n = System.Math.Pow(barrierHi(), 2 * n);
                var d1 = System.Math.Log(underlying() * U2n / (strike() * L2n)) / stdDeviation() + bsigma;
                var d2 = System.Math.Log(underlying() * U2n / (barrierHi() * L2n)) / stdDeviation() + bsigma;
                var d3 = System.Math.Log(System.Math.Pow(barrierLo(), 2 * n + 2) / (strike() * underlying() * U2n)) / stdDeviation() + bsigma;
                var d4 = System.Math.Log(System.Math.Pow(barrierLo(), 2 * n + 2) / (barrierHi() * underlying() * U2n)) / stdDeviation() + bsigma;

                acc1 += System.Math.Pow(System.Math.Pow(barrierHi(), n) / System.Math.Pow(barrierLo(), n), mu1) *
                        (f_.value(d1) - f_.value(d2)) -
                        System.Math.Pow(System.Math.Pow(barrierLo(), n + 1) / (System.Math.Pow(barrierHi(), n) * underlying()), mu1) *
                        (f_.value(d3) - f_.value(d4));

                acc2 += System.Math.Pow(System.Math.Pow(barrierHi(), n) / System.Math.Pow(barrierLo(), n), mu1 - 2) *
                        (f_.value(d1 - stdDeviation()) - f_.value(d2 - stdDeviation())) -
                        System.Math.Pow(System.Math.Pow(barrierLo(), n + 1) / (System.Math.Pow(barrierHi(), n) * underlying()), mu1 - 2) *
                        (f_.value(d3 - stdDeviation()) - f_.value(d4 - stdDeviation()));
            }

            var rend = System.Math.Exp(-dividendYield() * residualTime());
            var kov = underlying() * rend * acc1 - strike() * riskFreeDiscount() * acc2;
            return System.Math.Max(0.0, kov);

        }

        private double putKO()
        {

            var mu1 = 2 * costOfCarry() / volatilitySquared() + 1;
            var bsigma = (costOfCarry() + volatilitySquared() / 2.0) * residualTime() / stdDeviation();

            double acc1 = 0;
            double acc2 = 0;
            for (var n = -series_; n <= series_; ++n)
            {
                var L2n = System.Math.Pow(barrierLo(), 2 * n);
                var U2n = System.Math.Pow(barrierHi(), 2 * n);
                var y1 = System.Math.Log(underlying() * U2n / System.Math.Pow(barrierLo(), 2 * n + 1)) / stdDeviation() + bsigma;
                var y2 = System.Math.Log(underlying() * U2n / (strike() * L2n)) / stdDeviation() + bsigma;
                var y3 = System.Math.Log(System.Math.Pow(barrierLo(), 2 * n + 2) / (barrierLo() * underlying() * U2n)) / stdDeviation() + bsigma;
                var y4 = System.Math.Log(System.Math.Pow(barrierLo(), 2 * n + 2) / (strike() * underlying() * U2n)) / stdDeviation() + bsigma;

                acc1 += System.Math.Pow(System.Math.Pow(barrierHi(), n) / System.Math.Pow(barrierLo(), n), mu1 - 2) *
                        (f_.value(y1 - stdDeviation()) - f_.value(y2 - stdDeviation())) -
                        System.Math.Pow(System.Math.Pow(barrierLo(), n + 1) / (System.Math.Pow(barrierHi(), n) * underlying()), mu1 - 2) *
                        (f_.value(y3 - stdDeviation()) - f_.value(y4 - stdDeviation()));

                acc2 += System.Math.Pow(System.Math.Pow(barrierHi(), n) / System.Math.Pow(barrierLo(), n), mu1) *
                        (f_.value(y1) - f_.value(y2)) -
                        System.Math.Pow(System.Math.Pow(barrierLo(), n + 1) / (System.Math.Pow(barrierHi(), n) * underlying()), mu1) *
                        (f_.value(y3) - f_.value(y4));

            }

            var rend = System.Math.Exp(-dividendYield() * residualTime());
            var kov = strike() * riskFreeDiscount() * acc1 - underlying() * rend * acc2;
            return System.Math.Max(0.0, kov);

        }

        private double callKI() =>
            // Call KI equates to vanilla - callKO
            System.Math.Max(0.0, vanillaEquivalent() - callKO());

        private double putKI() =>
            // Put KI equates to vanilla - putKO
            System.Math.Max(0.0, vanillaEquivalent() - putKO());
    }
}
