/*
 Copyright (C) 2015 Thema Consulting SA
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.processes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Pricingengines.barrier
{
    //! Analytic pricing engine for double barrier binary options
    /*! This engine implements C.H.Hui series ("One-Touch Double Barrier
        Binary Option Values", Applied Financial Economics 6/1996), as
        described in "The complete guide to option pricing formulas 2nd Ed",
        E.G. Haug, McGraw-Hill, p.180

        The Knock In part of KI+KO and KO+KI options pays at hit, while the
        Double Knock In pays at end.
        This engine thus requires European esercise for Double Knock options,
        and American exercise for KIKO/KOKI.

        \ingroup barrierengines

        greeks are calculated by simple numeric derivation

        \test
        - the correctness of the returned value is tested by reproducing
          results available in literature.
    */

    // calc helper object
    [JetBrains.Annotations.PublicAPI] public class AnalyticDoubleBarrierBinaryEngine : DoubleBarrierOption.Engine
    {
        public AnalyticDoubleBarrierBinaryEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            if (arguments_.barrierType == DoubleBarrier.Type.KIKO ||
                arguments_.barrierType == DoubleBarrier.Type.KOKI)
            {
                var ex = arguments_.exercise as AmericanExercise;
                Utils.QL_REQUIRE(ex != null, () => "KIKO/KOKI options must have American exercise");
                Utils.QL_REQUIRE(ex.dates()[0] <=
                                 process_.blackVolatility().currentLink().referenceDate(),
                                 () => "American option with window exercise not handled yet");
            }
            else
            {
                var ex = arguments_.exercise as EuropeanExercise;
                Utils.QL_REQUIRE(ex != null, () => "non-European exercise given");
            }
            var payoff = arguments_.payoff as CashOrNothingPayoff;
            Utils.QL_REQUIRE(payoff != null, () => "a cash-or-nothing payoff must be given");

            var spot = process_.stateVariable().currentLink().value();
            Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");

            var variance =
               process_.blackVolatility().currentLink().blackVariance(
                  arguments_.exercise.lastDate(),
                  payoff.strike());
            var barrier_lo = arguments_.barrier_lo.Value;
            var barrier_hi = arguments_.barrier_hi.Value;
            var barrierType = arguments_.barrierType;
            Utils.QL_REQUIRE(barrier_lo > 0.0,
                             () => "positive low barrier value required");
            Utils.QL_REQUIRE(barrier_hi > 0.0,
                             () => "positive high barrier value required");
            Utils.QL_REQUIRE(barrier_lo < barrier_hi,
                             () => "barrier_lo must be < barrier_hi");
            Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockIn ||
                             barrierType == DoubleBarrier.Type.KnockOut ||
                             barrierType == DoubleBarrier.Type.KIKO ||
                             barrierType == DoubleBarrier.Type.KOKI,
                             () => "Unsupported barrier ExerciseType");

            // degenerate cases
            switch (barrierType)
            {
                case DoubleBarrier.Type.KnockOut:
                    if (spot <= barrier_lo || spot >= barrier_hi)
                    {
                        // knocked out, no value
                        results_.value = 0;
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    break;

                case DoubleBarrier.Type.KnockIn:
                    if (spot <= barrier_lo || spot >= barrier_hi)
                    {
                        // knocked in - pays
                        results_.value = payoff.cashPayoff();
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    break;

                case DoubleBarrier.Type.KIKO:
                    if (spot >= barrier_hi)
                    {
                        // knocked out, no value
                        results_.value = 0;
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    else if (spot <= barrier_lo)
                    {
                        // knocked in, pays
                        results_.value = payoff.cashPayoff();
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    break;

                case DoubleBarrier.Type.KOKI:
                    if (spot <= barrier_lo)
                    {
                        // knocked out, no value
                        results_.value = 0;
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    else if (spot >= barrier_hi)
                    {
                        // knocked in, pays
                        results_.value = payoff.cashPayoff();
                        results_.delta = 0;
                        results_.gamma = 0;
                        results_.vega = 0;
                        results_.rho = 0;
                        return;
                    }
                    break;
            }

            var helper = new AnalyticDoubleBarrierBinaryEngineHelper(process_,
                  payoff, arguments_);
            switch (barrierType)
            {
                case DoubleBarrier.Type.KnockOut:
                case DoubleBarrier.Type.KnockIn:
                    results_.value = helper.payoffAtExpiry(spot, variance, barrierType);
                    break;

                case DoubleBarrier.Type.KIKO:
                case DoubleBarrier.Type.KOKI:
                    results_.value = helper.payoffKIKO(spot, variance, barrierType);
                    break;
                default:
                    results_.value = null;
                    break;
            }
        }

        protected GeneralizedBlackScholesProcess process_;
    }
}
