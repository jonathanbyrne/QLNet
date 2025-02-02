﻿//  Copyright (C) 2015 Thema Consulting SA
//  Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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
using QLNet.Math;
using QLNet.Methods.lattices;
using QLNet.Processes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;

namespace QLNet.PricingEngines.barrier
{
    //! Pricing engine for double barrier options using binomial trees
    /*! \ingroup barrierengines

        \note This engine requires a the discretized option classes.
        By default uses a standard binomial implementation, but it can
        also work with DiscretizedDermanKaniDoubleBarrierOption to
        implement a Derman-Kani optimization.

        \test the correctness of the returned values is tested by
              checking it against analytic results.
    */
    [PublicAPI]
    public class BinomialDoubleBarrierEngine : DoubleBarrierOption.Engine
    {
        public delegate DiscretizedAsset GetAsset(DoubleBarrierOption.Arguments args, StochasticProcess process, TimeGrid grid = null);

        public delegate ITree GetTree(StochasticProcess1D process, double end, int steps, double strike);

        private GetAsset getAsset_;
        private GetTree getTree_;
        private int maxTimeSteps_;
        private GeneralizedBlackScholesProcess process_;
        private int timeSteps_;

        /*! \param maxTimeSteps is used to limit timeSteps when using Boyle-Lau
                     optimization. If zero (the default) the maximum number of
                     steps is calculated by an heuristic: anything when < 1000,
                     otherwise no more than 5*timeSteps.
                     If maxTimeSteps is equal to timeSteps Boyle-Lau is disabled.
                     Likewise if the lattice is not CoxRossRubinstein Boyle-Lau is
                     disabled and maxTimeSteps ignored.
        */
        public BinomialDoubleBarrierEngine(GetTree getTree, GetAsset getAsset,
            GeneralizedBlackScholesProcess process, int timeSteps, int maxTimeSteps = 0)
        {
            process_ = process;
            timeSteps_ = timeSteps;
            maxTimeSteps_ = maxTimeSteps;
            getTree_ = getTree;
            getAsset_ = getAsset;

            QLNet.Utils.QL_REQUIRE(timeSteps > 0, () =>
                "timeSteps must be positive, " + timeSteps + " not allowed");
            QLNet.Utils.QL_REQUIRE(maxTimeSteps == 0 || maxTimeSteps >= timeSteps, () =>
                "maxTimeSteps must be zero or greater than or equal to timeSteps, " + maxTimeSteps + " not allowed");
            if (maxTimeSteps_ == 0)
            {
                maxTimeSteps_ = System.Math.Max(1000, timeSteps_ * 5);
            }

            process_.registerWith(update);
        }

        public override void calculate()
        {
            var rfdc = process_.riskFreeRate().link.dayCounter();
            var divdc = process_.dividendYield().link.dayCounter();
            var voldc = process_.blackVolatility().link.dayCounter();
            var volcal = process_.blackVolatility().link.calendar();

            var s0 = process_.stateVariable().link.value();
            QLNet.Utils.QL_REQUIRE(s0 > 0.0, () => "negative or null underlying given");
            var v = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), s0);
            var maturityDate = arguments_.exercise.lastDate();
            var r = process_.riskFreeRate().link.zeroRate(maturityDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).value();
            var q = process_.dividendYield().link.zeroRate(maturityDate, divdc, Compounding.Continuous, Frequency.NoFrequency).value();
            var referenceDate = process_.riskFreeRate().link.referenceDate();

            // binomial trees with constant coefficient
            var flatRiskFree = new Handle<YieldTermStructure>(new FlatForward(referenceDate, r, rfdc));
            var flatDividends = new Handle<YieldTermStructure>(new FlatForward(referenceDate, q, divdc));
            var flatVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(referenceDate, volcal, v, voldc));

            var payoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "non-striked payoff given");

            var maturity = rfdc.yearFraction(referenceDate, maturityDate);

            StochasticProcess1D bs = new GeneralizedBlackScholesProcess(process_.stateVariable(),
                flatDividends, flatRiskFree, flatVol);

            var grid = new TimeGrid(maturity, timeSteps_);

            var tree = getTree_(bs, maturity, timeSteps_, payoff.strike());

            var lattice = new BlackScholesLattice<ITree>(tree, r, maturity, timeSteps_);

            var option = getAsset_(arguments_, process_, grid);
            option.initialize(lattice, maturity);

            // Partial derivatives calculated from various points in the
            // binomial tree
            // (see J.C.Hull, "Options, Futures and other derivatives", 6th edition, pp 397/398)

            // Rollback to third-last step, and get underlying prices (s2) &
            // option values (p2) at this point
            option.rollback(grid[2]);
            var va2 = new Vector(option.values());
            QLNet.Utils.QL_REQUIRE(va2.size() == 3, () => "Expect 3 nodes in grid at second step");
            var p2u = va2[2]; // up
            var p2m = va2[1]; // mid
            var p2d = va2[0]; // down (low)
            var s2u = lattice.underlying(2, 2); // up price
            var s2m = lattice.underlying(2, 1); // middle price
            var s2d = lattice.underlying(2, 0); // down (low) price

            // calculate gamma by taking the first derivate of the two deltas
            var delta2u = (p2u - p2m) / (s2u - s2m);
            var delta2d = (p2m - p2d) / (s2m - s2d);
            var gamma = (delta2u - delta2d) / ((s2u - s2d) / 2);

            // Rollback to second-last step, and get option values (p1) at
            // this point
            option.rollback(grid[1]);
            var va = new Vector(option.values());
            QLNet.Utils.QL_REQUIRE(va.size() == 2, () => "Expect 2 nodes in grid at first step");
            var p1u = va[1];
            var p1d = va[0];
            var s1u = lattice.underlying(1, 1); // up (high) price
            var s1d = lattice.underlying(1, 0); // down (low) price

            var delta = (p1u - p1d) / (s1u - s1d);

            // Finally, rollback to t=0
            option.rollback(0.0);
            var p0 = option.presentValue();

            // Store results
            results_.value = p0;
            results_.delta = delta;
            results_.gamma = gamma;
            // theta can be approximated by calculating the numerical derivative
            // between mid value at third-last step and at t0. The underlying price
            // is the same, only time varies.
            results_.theta = (p2m - p0) / grid[2];
        }
    }
}
