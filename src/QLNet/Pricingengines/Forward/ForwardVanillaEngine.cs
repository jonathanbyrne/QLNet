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
using QLNet.Processes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;

namespace QLNet.PricingEngines.Forward
{
    //! %Forward engine for vanilla options
    /*! \ingroup forwardengines

        \test
        - the correctness of the returned value is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing numerical derivatives.
    */
    [PublicAPI]
    public class ForwardVanillaEngine : GenericEngine<ForwardVanillaOption.Arguments, OneAssetOption.Results>
    {
        public delegate IPricingEngine GetOriginalEngine(GeneralizedBlackScholesProcess process);

        protected GetOriginalEngine getOriginalEngine_;
        protected QLNet.Option.Arguments originalArguments_;
        protected IPricingEngine originalEngine_;
        protected OneAssetOption.Results originalResults_;
        protected GeneralizedBlackScholesProcess process_;

        public ForwardVanillaEngine(GeneralizedBlackScholesProcess process, GetOriginalEngine getEngine)
        {
            process_ = process;
            process_.registerWith(update);
            getOriginalEngine_ = getEngine;
        }

        public override void calculate()
        {
            setup();
            originalEngine_.calculate();
            getOriginalResults();
        }

        protected virtual void getOriginalResults()
        {
            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var resetTime = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.resetDate);
            var discQ = process_.dividendYield().link.discount(arguments_.resetDate);

            results_.value = discQ * originalResults_.value;
            // I need the strike derivative here ...
            if (originalResults_.delta != null && originalResults_.strikeSensitivity != null)
            {
                results_.delta = discQ * (originalResults_.delta +
                                          arguments_.moneyness * originalResults_.strikeSensitivity);
            }

            results_.gamma = 0.0;
            results_.theta = process_.dividendYield().link.zeroRate(arguments_.resetDate, divdc, Compounding.Continuous, Frequency.NoFrequency).value()
                             * results_.value;
            if (originalResults_.vega != null)
            {
                results_.vega = discQ * originalResults_.vega;
            }

            if (originalResults_.rho != null)
            {
                results_.rho = discQ * originalResults_.rho;
            }

            if (originalResults_.dividendRho != null)
            {
                results_.dividendRho = -resetTime * results_.value
                                       + discQ * originalResults_.dividendRho;
            }
        }

        protected void setup()
        {
            var argumentsPayoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(argumentsPayoff != null, () => "wrong payoff given");

            StrikedTypePayoff payoff = new PlainVanillaPayoff(argumentsPayoff.optionType(),
                arguments_.moneyness * process_.x0());

            // maybe the forward value is "better", in some fashion
            // the right level is needed in order to interpolate
            // the vol
            var spot = process_.stateVariable();
            QLNet.Utils.QL_REQUIRE(spot.link.value() >= 0.0, () => "negative or null underlting given");
            var dividendYield = new Handle<YieldTermStructure>(
                new ImpliedTermStructure(process_.dividendYield(), arguments_.resetDate));
            var riskFreeRate = new Handle<YieldTermStructure>(
                new ImpliedTermStructure(process_.riskFreeRate(), arguments_.resetDate));
            // The following approach is ok if the vol is at most
            // time dependant. It is plain wrong if it is asset dependant.
            // In the latter case the right solution would be stochastic
            // volatility or at least local volatility (which unfortunately
            // implies an unrealistic time-decreasing smile)
            var blackVolatility = new Handle<BlackVolTermStructure>(
                new ImpliedVolTermStructure(process_.blackVolatility(), arguments_.resetDate));

            var fwdProcess = new GeneralizedBlackScholesProcess(spot, dividendYield,
                riskFreeRate, blackVolatility);

            originalEngine_ = getOriginalEngine_(fwdProcess);
            originalEngine_.reset();

            originalArguments_ = originalEngine_.getArguments() as QLNet.Option.Arguments;
            QLNet.Utils.QL_REQUIRE(originalArguments_ != null, () => "wrong engine ExerciseType");
            originalResults_ = originalEngine_.getResults() as OneAssetOption.Results;
            QLNet.Utils.QL_REQUIRE(originalResults_ != null, () => "wrong engine ExerciseType");

            originalArguments_.payoff = payoff;
            originalArguments_.exercise = arguments_.exercise;

            originalArguments_.validate();
        }
    }
}
