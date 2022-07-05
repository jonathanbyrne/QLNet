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

using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Models.Equity;

namespace QLNet.Pricingengines.vanilla
{
    //! Heston-model engine for European options based on analytic expansions
    /*! References:

        M Forde, A Jacquier, R Lee, The small-time smile and term
        structure of implied volatility under the Heston model
        SIAM Journal on Financial Mathematics, 2012 - SIAM

        M Lorig, S Pagliarani, A Pascucci, Explicit implied vols for
        multifactor local-stochastic vol models
        arXiv preprint arXiv:1306.5447v3, 2014 - arxiv.org

        \ingroup vanillaengines
    */
    [PublicAPI]
    public class HestonExpansionEngine : GenericModelEngine<HestonModel, QLNet.Option.Arguments,
        OneAssetOption.Results>
    {
        public enum HestonExpansionFormula
        {
            LPP2,
            LPP3,
            Forde
        }

        private HestonExpansionFormula formula_;

        public HestonExpansionEngine(HestonModel model, HestonExpansionFormula formula)
            : base(model)
        {
            formula_ = formula;
        }

        public override void calculate()
        {
            // this is a european option pricer
            Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () => "not an European option");

            // plain vanilla
            var payoff = arguments_.payoff as PlainVanillaPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "non plain vanilla payoff given");

            var process = model_.link.process();

            var riskFreeDiscount = process.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            var dividendDiscount = process.dividendYield().link.discount(arguments_.exercise.lastDate());

            var spotPrice = process.s0().link.value();
            Utils.QL_REQUIRE(spotPrice > 0.0, () => "negative or null underlying given");

            var strikePrice = payoff.strike();
            var term = process.time(arguments_.exercise.lastDate());

            //possible optimization:
            //  if term=lastTerm & model=lastModel & formula=lastApprox, reuse approx.
            var forward = spotPrice * dividendDiscount / riskFreeDiscount;
            double vol = 0;
            switch (formula_)
            {
                case HestonExpansionFormula.LPP2:
                {
                    var expansion = new LPP2HestonExpansion(model_.link.kappa(), model_.link.theta(),
                        model_.link.sigma(), model_.link.v0(), model_.link.rho(), term);
                    vol = expansion.impliedVolatility(strikePrice, forward);
                    break;
                }
                case HestonExpansionFormula.LPP3:
                {
                    var expansion = new LPP3HestonExpansion(model_.link.kappa(), model_.link.theta(),
                        model_.link.sigma(), model_.link.v0(), model_.link.rho(), term);
                    vol = expansion.impliedVolatility(strikePrice, forward);
                    break;
                }
                case HestonExpansionFormula.Forde:
                {
                    var expansion = new FordeHestonExpansion(model_.link.kappa(), model_.link.theta(),
                        model_.link.sigma(), model_.link.v0(), model_.link.rho(), term);
                    vol = expansion.impliedVolatility(strikePrice, forward);
                    break;
                }
                default:
                    Utils.QL_FAIL("unknown expansion formula");
                    break;
            }

            var price = Utils.blackFormula(payoff, forward, vol * System.Math.Sqrt(term), riskFreeDiscount);
            results_.value = price;
        }
    }

    /*! Interface to represent some Heston expansion formula.
         During calibration, it would typically be initialized once per
         implied volatility surface slice, then calls for each surface
         strike to impliedVolatility(strike, forward) would be
         performed.
     */
    /*! Lorig Pagliarani Pascucci expansion of order-2 for the Heston model.
        During calibration, it can be initialized once per expiry, and
        called many times with different strikes.  The formula is also
        available in the Mathematica notebook from the authors at
        http://explicitsolutions.wordpress.com/
     */
    /*! Lorig Pagliarani Pascucci expansion of order-3 for the Heston model.
        During calibration, it can be initialized once per expiry, and
        called many times with different strikes.  The formula is also
        available in the Mathematica notebook from the authors at
        http://explicitsolutions.wordpress.com/
    */
    /*! Small-time expansion from
        "The small-time smile and term structure of implied volatility
        under the Heston model" M Forde, A Jacquier, R Lee - SIAM
        Journal on Financial Mathematics, 2012 - SIAM
    */
}
