/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Instruments.Bonds;
using QLNet.Models;
using QLNet.Termstructures;
using QLNet.Time;

// Numerical lattice engines for callable/puttable bonds
namespace QLNet.PricingEngines.Bond
{
    //! Numerical lattice engine for callable fixed rate bonds
    /*! \ingroup callablebondengines */
    [PublicAPI]
    public class TreeCallableFixedRateBondEngine : LatticeShortRateModelEngine<CallableBond.Arguments, CallableBond.Results>
    {
        private Handle<YieldTermStructure> termStructure_;

        /* Constructors
            \note the term structure is only needed when the short-rate
                  model cannot provide one itself.
        */
        public TreeCallableFixedRateBondEngine(ShortRateModel model, int timeSteps,
            Handle<YieldTermStructure> termStructure)
            : base(model, timeSteps)
        {
            termStructure_ = termStructure;
            termStructure_.registerWith(update);
        }

        public TreeCallableFixedRateBondEngine(ShortRateModel model, TimeGrid timeGrid,
            Handle<YieldTermStructure> termStructure)
            : base(model, timeGrid)
        {
            termStructure_ = termStructure;
            termStructure_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(model_ != null, () => "no model specified");

            Date referenceDate;
            DayCounter dayCounter;

            var tsmodel = (ITermStructureConsistentModel)model_.link;
            if (tsmodel != null)
            {
                referenceDate = tsmodel.termStructure().link.referenceDate();
                dayCounter = tsmodel.termStructure().link.dayCounter();
            }
            else
            {
                referenceDate = termStructure_.link.referenceDate();
                dayCounter = termStructure_.link.dayCounter();
            }

            var callableBond = new DiscretizedCallableFixedRateBond(arguments_, referenceDate, dayCounter);
            Lattice lattice;

            if (lattice_ != null)
            {
                lattice = lattice_;
            }
            else
            {
                var times = callableBond.mandatoryTimes();
                var timeGrid = new TimeGrid(times, times.Count, timeSteps_);
                lattice = model_.link.tree(timeGrid);
            }

            var redemptionTime = dayCounter.yearFraction(referenceDate, arguments_.redemptionDate);
            callableBond.initialize(lattice, redemptionTime);
            callableBond.rollback(0.0);
            results_.value = results_.settlementValue = callableBond.presentValue();
        }
    }
}
