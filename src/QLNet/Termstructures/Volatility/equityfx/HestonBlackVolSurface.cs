/*
 Copyright (C) 2017 Francois Botha (igitur@gmail.com)

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

using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Models.Equity;
using QLNet.Pricingengines.vanilla;
using QLNet.processes;
using QLNet.Time;
using System;
using QLNet.Instruments;
using QLNet.Time.Calendars;

namespace QLNet.Termstructures.Volatility.equityfx
{
    //! Black volatility surface backed by Heston model
    [JetBrains.Annotations.PublicAPI] public class HestonBlackVolSurface : BlackVolTermStructure
    {
        private Handle<HestonModel> hestonModel_;
        private AnalyticHestonEngine.Integration integration_;

        public HestonBlackVolSurface(Handle<HestonModel> hestonModel)
           : base(hestonModel.link.process().riskFreeRate().link.referenceDate(),
                  new NullCalendar(),
                  BusinessDayConvention.Following,
                  hestonModel.link.process().riskFreeRate().link.dayCounter())
        {
            hestonModel_ = hestonModel;
            integration_ = AnalyticHestonEngine.Integration.gaussLaguerre(164);

            hestonModel_.registerWith(update);
        }

        public override Date maxDate() => Date.maxDate();

        public override double maxStrike() => double.MaxValue;

        public override double minStrike() => 0.0;

        protected override double blackVarianceImpl(double t, double strike) => System.Math.Pow(blackVolImpl(t, strike), 2.0) * t;

        protected override double blackVolImpl(double t, double strike)
        {
            var process = hestonModel_.link.process();

            var df = process.riskFreeRate().link.discount(t, true);
            var div = process.dividendYield().link.discount(t, true);
            var spotPrice = process.s0().link.value();

            var fwd = spotPrice
                      * process.dividendYield().link.discount(t, true)
                      / process.riskFreeRate().link.discount(t, true);

            var payoff = new PlainVanillaPayoff(fwd > strike ? QLNet.Option.Type.Put : QLNet.Option.Type.Call, strike);

            var kappa = hestonModel_.link.kappa();
            var theta = hestonModel_.link.theta();
            var rho = hestonModel_.link.rho();
            var sigma = hestonModel_.link.sigma();
            var v0 = hestonModel_.link.v0();

            var cpxLogFormula = AnalyticHestonEngine.ComplexLogFormula.Gatheral;

            AnalyticHestonEngine hestonEnginePtr = null;

            double? npv = null;
            var evaluations = 0;

            AnalyticHestonEngine.doCalculation(
               df, div, spotPrice, strike, t,
               kappa, theta, sigma, v0, rho,
               payoff, integration_, cpxLogFormula,
               hestonEnginePtr, ref npv, ref evaluations);

            if (npv <= 0.0)
                return System.Math.Sqrt(theta);

            var solver = new Brent();
            solver.setMaxEvaluations(10000);
            var guess = System.Math.Sqrt(theta);
            var accuracy = Const.QL_EPSILON;

            var f = new ImpliedVolHelper(payoff.optionType(), strike, fwd, t, df, npv.Value);

            return solver.solve(f, accuracy, guess, 0.01);
        }

        protected class ImpliedVolHelper : ISolver1d
        {
            private double discount_;
            private double forward_;
            private double maturity_;
            private QLNet.Option.Type optionType_;
            private double strike_;
            private double targetValue_;

            public ImpliedVolHelper(Option.Type optionType, double strike,
                                    double forward, double maturity,
                                    double discount, double targetValue)
            {
                optionType_ = optionType;
                strike_ = strike;
                forward_ = forward;
                maturity_ = maturity;
                discount_ = discount;
                targetValue_ = targetValue;
            }

            public override double value(double x) => blackValue(optionType_, strike_, forward_, maturity_, x, discount_, targetValue_);

            private double blackValue(Option.Type optionType, double strike,
                                      double forward, double maturity,
                                      double vol, double discount, double npv) =>
                Utils.blackFormula(optionType, strike, forward,
                    System.Math.Max(0.0, vol) * System.Math.Sqrt(maturity),
                    discount) - npv;
        }
    }
}
