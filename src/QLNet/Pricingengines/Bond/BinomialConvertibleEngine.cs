//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Instruments.Bonds;
using QLNet.Methods.lattices;
using QLNet.processes;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;

namespace QLNet.Pricingengines.Bond
{
    /// <summary>
    ///     Binomial Tsiveriotis-Fernandes engine for convertible bonds
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [PublicAPI]
    public class BinomialConvertibleEngine<T> : ConvertibleBond.option.Engine where T : ITree, ITreeFactory<T>, new()
    {
        private GeneralizedBlackScholesProcess process_;
        private int timeSteps_;

        public BinomialConvertibleEngine(GeneralizedBlackScholesProcess process, int timeSteps)
        {
            process_ = process;
            timeSteps_ = timeSteps;

            Utils.QL_REQUIRE(timeSteps > 0, () => " timeSteps must be positive");
            process_.registerWith(update);
        }

        public override void calculate()
        {
            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();
            var volcal = process_.blackVolatility().link.calendar();

            var s0 = process_.x0();
            Utils.QL_REQUIRE(s0 > 0.0, () => "negative or null underlying");
            var v = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), s0);
            var maturityDate = arguments_.exercise.lastDate();
            var riskFreeRate = process_.riskFreeRate().link.zeroRate(maturityDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).value();
            var q = process_.dividendYield().link.zeroRate(maturityDate, divdc, Compounding.Continuous, Frequency.NoFrequency).value();
            var referenceDate = process_.riskFreeRate().link.referenceDate();

            // substract dividends

            var args = arguments_;

            for (var i = 0; i < args.dividends.Count; i++)
            {
                if (args.dividends[i].date() >= referenceDate)
                {
                    s0 -= args.dividends[i].amount() * process_.riskFreeRate().link.discount(args.dividends[i].date());
                }

                Utils.QL_REQUIRE(s0 > 0.0, () => "negative value after substracting dividends");
            }

            // binomial trees with constant coefficient

            var underlying = new Handle<Quote>(new SimpleQuote(s0));
            var flatRiskFree =
                new Handle<YieldTermStructure>(new FlatForward(referenceDate, riskFreeRate, rfdc));
            var flatDividends =
                new Handle<YieldTermStructure>(new FlatForward(referenceDate, q, divdc));
            var flatVol =
                new Handle<BlackVolTermStructure>(new BlackConstantVol(referenceDate, volcal, v, voldc));
            var payoff = args.payoff as PlainVanillaPayoff;

            Utils.QL_REQUIRE(payoff != null, () => " non-plain payoff given ");

            var maturity = rfdc.yearFraction(args.settlementDate, maturityDate);
            var bs =
                new GeneralizedBlackScholesProcess(underlying, flatDividends, flatRiskFree, flatVol);

            var Tree = new T().factory(bs, maturity, timeSteps_, payoff.strike());

            var creditSpread = args.creditSpread.link.value();

            var lattice = new TsiveriotisFernandesLattice<T>(Tree, riskFreeRate,
                maturity, timeSteps_, creditSpread, v, q);
            var convertible = new DiscretizedConvertible(args, bs, new TimeGrid(maturity, timeSteps_));
            convertible.initialize(lattice, maturity);
            convertible.rollback(0.0);
            results_.value = convertible.presentValue();

            Utils.QL_REQUIRE(results_.value < double.MaxValue, () => "floating-point overflow on tree grid");
        }
    }
}
