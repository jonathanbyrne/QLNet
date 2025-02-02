﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)

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
using QLNet.Instruments;
using QLNet.Models.Shortrate.Twofactorsmodels;
using QLNet.PricingEngines.Swap;

namespace QLNet.PricingEngines.swaption
{
    // Swaption pricing engine for two-factor additive Gaussian Model G2 + +
    //! %Swaption priced by means of the Black formula
    /*! \ingroup swaptionengines

        \warning The engine assumes that the exercise date equals the
                 start date of the passed swap.
    */
    [PublicAPI]
    public class G2SwaptionEngine : GenericModelEngine<G2, Swaption.Arguments,
        Instrument.Results>
    {
        private int intervals_;
        private double range_;

        // range is the number of standard deviations to use in the
        // exponential term of the integral for the european swaption.
        // intervals is the number of intervals to use in the integration.
        public G2SwaptionEngine(G2 model,
            double range,
            int intervals)
            : base(model)
        {
            range_ = range;
            intervals_ = intervals;
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(arguments_.settlementType == Settlement.Type.Physical, () =>
                "cash-settled swaptions not priced with G2 engine");

            // adjust the fixed rate of the swap for the spread on the
            // floating leg (which is not taken into account by the
            // model)
            var swap = arguments_.swap;
            swap.setPricingEngine(new DiscountingSwapEngine(model_.link.termStructure()));
            var correction = swap.spread *
                             System.Math.Abs(swap.floatingLegBPS() / swap.fixedLegBPS());
            var fixedRate = swap.fixedRate - correction;

            results_.value = model_.link.swaption(arguments_, fixedRate,
                range_, intervals_);
        }
    }
}
