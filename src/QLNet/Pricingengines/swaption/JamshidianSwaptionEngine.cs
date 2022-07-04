/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Models;
using QLNet.Models.Shortrate;
using QLNet.Pricingengines;
using QLNet.Termstructures;
using QLNet.Time;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Pricingengines.swaption
{
    //! Jamshidian swaption engine
    /*! \ingroup swaptionengines

        \warning The engine assumes that the exercise date equals the
                 start date of the passed swap.
    */

    [JetBrains.Annotations.PublicAPI] public class JamshidianSwaptionEngine : GenericModelEngine<OneFactorAffineModel,
      Swaption.Arguments,
      Instrument.Results>
    {

        /*! \note the term structure is only needed when the short-rate
                 model cannot provide one itself.
        */
        public JamshidianSwaptionEngine(OneFactorAffineModel model,
                                        Handle<YieldTermStructure> termStructure)
           : base(model)
        {
            termStructure_ = termStructure;
            termStructure_.registerWith(update);
        }

        public JamshidianSwaptionEngine(OneFactorAffineModel model)
           : this(model, new Handle<YieldTermStructure>())
        { }

        private Handle<YieldTermStructure> termStructure_;

        [JetBrains.Annotations.PublicAPI] public class rStarFinder : ISolver1d
        {

            public rStarFinder(OneFactorAffineModel model,
                               double nominal,
                               double maturity,
                               List<double> fixedPayTimes,
                               List<double> amounts)
            {
                strike_ = nominal;
                maturity_ = maturity;
                times_ = fixedPayTimes;
                amounts_ = amounts;
                model_ = model;
            }

            public override double value(double x)
            {
                var value = strike_;
                var size = times_.Count;
                for (var i = 0; i < size; i++)
                {
                    var dbValue =
                       model_.discountBond(maturity_, times_[i], x);
                    value -= amounts_[i] * dbValue;
                }
                return value;
            }

            private double strike_;
            private double maturity_;
            private List<double> times_;
            private List<double> amounts_;
            private OneFactorAffineModel model_;
        }

        public override void calculate()
        {
            Utils.QL_REQUIRE(arguments_.settlementMethod != Settlement.Method.ParYieldCurve, () =>
                             "cash-settled (ParYieldCurve) swaptions not priced by Jamshidian engine");

            Utils.QL_REQUIRE(arguments_.exercise.ExerciseType() == Exercise.Type.European, () =>
                             "cannot use the Jamshidian decomposition on exotic swaptions");

            Utils.QL_REQUIRE(arguments_.swap.spread.IsEqual(0.0), () =>
                             "non zero spread (" + arguments_.swap.spread + ") not allowed");

            Date referenceDate;
            DayCounter dayCounter;

            var tsmodel = (ITermStructureConsistentModel)model_.link;
            try
            {
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
            }
            catch
            {
                referenceDate = termStructure_.link.referenceDate();
                dayCounter = termStructure_.link.dayCounter();
            }

            List<double> amounts = new InitializedList<double>(arguments_.fixedCoupons.Count);
            for (var i = 0; i < amounts.Count; i++)
                amounts[i] = arguments_.fixedCoupons[i];
            amounts[amounts.Count - 1] = amounts.Last() + arguments_.nominal;

            var maturity = dayCounter.yearFraction(referenceDate,
                                                      arguments_.exercise.date(0));

            List<double> fixedPayTimes = new InitializedList<double>(arguments_.fixedPayDates.Count);
            for (var i = 0; i < fixedPayTimes.Count; i++)
                fixedPayTimes[i] =
                   dayCounter.yearFraction(referenceDate,
                                           arguments_.fixedPayDates[i]);

            var finder = new rStarFinder(model_, arguments_.nominal, maturity,
                                                 fixedPayTimes, amounts);
            var s1d = new Brent();
            var minStrike = -10.0;
            var maxStrike = 10.0;
            s1d.setMaxEvaluations(10000);
            s1d.setLowerBound(minStrike);
            s1d.setUpperBound(maxStrike);
            var rStar = s1d.solve(finder, 1e-8, 0.05, minStrike, maxStrike);

            var w = arguments_.type == VanillaSwap.Type.Payer ?
                            QLNet.Option.Type.Put : QLNet.Option.Type.Call;
            var size = arguments_.fixedCoupons.Count;

            var value = 0.0;
            for (var i = 0; i < size; i++)
            {
                var fixedPayTime =
                   dayCounter.yearFraction(referenceDate,
                                           arguments_.fixedPayDates[i]);
                var strike = model_.link.discountBond(maturity,
                                                         fixedPayTime,
                                                         rStar);
                var dboValue = model_.link.discountBondOption(
                                     w, strike, maturity,
                                     fixedPayTime);
                value += amounts[i] * dboValue;
            }
            results_.value = value;
        }
    }
}
