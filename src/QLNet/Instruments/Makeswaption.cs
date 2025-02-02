﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
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

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.PricingEngines.Swap;
using QLNet.Time;

namespace QLNet.Instruments
{
    /// <summary>
    ///     Helper class to instantiate standard market swaption.
    /// </summary>
    [PublicAPI]
    public class MakeSwaption
    {
        private Settlement.Type delivery_;
        private IPricingEngine engine_;
        private Exercise exercise_;
        private Date exerciseDate_;
        private Date fixingDate_;
        private double nominal_;
        private BusinessDayConvention optionConvention_;
        private Period optionTenor_;
        private Settlement.Method settlementMethod_;
        private double? strike_;
        private SwapIndex swapIndex_;
        private VanillaSwap underlyingSwap_;
        private VanillaSwap.Type underlyingType_;

        public MakeSwaption(SwapIndex swapIndex,
            Period optionTenor,
            double? strike = null)
        {
            swapIndex_ = swapIndex;
            delivery_ = Settlement.Type.Physical;
            settlementMethod_ = Settlement.Method.PhysicalOTC;
            optionTenor_ = optionTenor;
            optionConvention_ = BusinessDayConvention.ModifiedFollowing;
            fixingDate_ = null;
            strike_ = strike;
            underlyingType_ = VanillaSwap.Type.Payer;
            nominal_ = 1.0;
        }

        public MakeSwaption(SwapIndex swapIndex,
            Date fixingDate,
            double? strike = null)
        {
            swapIndex_ = swapIndex;
            delivery_ = Settlement.Type.Physical;
            settlementMethod_ = Settlement.Method.PhysicalOTC;
            optionConvention_ = BusinessDayConvention.ModifiedFollowing;
            fixingDate_ = fixingDate;
            strike_ = strike;
            underlyingType_ = VanillaSwap.Type.Payer;
            nominal_ = 1.0;
        }

        // swap creator
        public static implicit operator Swaption(MakeSwaption o) => o.value();

        public Swaption value()
        {
            var evaluationDate = Settings.evaluationDate();
            var fixingCalendar = swapIndex_.fixingCalendar();
            fixingDate_ = fixingCalendar.advance(evaluationDate, optionTenor_, optionConvention_);

            if (exerciseDate_ == null)
            {
                exercise_ = new EuropeanExercise(fixingDate_);
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(exerciseDate_ <= fixingDate_, () =>
                    "exercise date (" + exerciseDate_ + ") must be less " + "than or equal to fixing date (" + fixingDate_ + ")");
                exercise_ = new EuropeanExercise(exerciseDate_);
            }

            double usedStrike;
            if (strike_ == null)
            {
                // ATM on the forecasting curve
                QLNet.Utils.QL_REQUIRE(!swapIndex_.forwardingTermStructure().empty(), () =>
                    "no forecasting term structure set to " + swapIndex_.name());
                var temp = swapIndex_.underlyingSwap(fixingDate_);
                temp.setPricingEngine(new DiscountingSwapEngine(swapIndex_.forwardingTermStructure()));
                usedStrike = temp.fairRate();
            }
            else
            {
                usedStrike = strike_.Value;
            }

            var bdc = swapIndex_.fixedLegConvention();
            underlyingSwap_ = new MakeVanillaSwap(swapIndex_.tenor(),
                    swapIndex_.iborIndex(),
                    usedStrike)
                .withEffectiveDate(swapIndex_.valueDate(fixingDate_))
                .withFixedLegCalendar(swapIndex_.fixingCalendar())
                .withFixedLegDayCount(swapIndex_.dayCounter())
                .withFixedLegConvention(bdc)
                .withFixedLegTerminationDateConvention(bdc)
                .withType(underlyingType_)
                .withNominal(nominal_);

            var swaption = new Swaption(underlyingSwap_, exercise_, delivery_, settlementMethod_);
            swaption.setPricingEngine(engine_);
            return swaption;
        }

        public MakeSwaption withExerciseDate(Date exerciseDate)
        {
            exerciseDate_ = exerciseDate;
            return this;
        }

        public MakeSwaption withNominal(double n)
        {
            nominal_ = n;
            return this;
        }

        public MakeSwaption withOptionConvention(BusinessDayConvention bdc)
        {
            optionConvention_ = bdc;
            return this;
        }

        public MakeSwaption withPricingEngine(IPricingEngine engine)
        {
            engine_ = engine;
            return this;
        }

        public MakeSwaption withSettlementMethod(Settlement.Method settlementMethod)
        {
            settlementMethod_ = settlementMethod;
            return this;
        }

        public MakeSwaption withSettlementType(Settlement.Type delivery)
        {
            delivery_ = delivery;
            return this;
        }

        public MakeSwaption withUnderlyingType(VanillaSwap.Type type)
        {
            underlyingType_ = type;
            return this;
        }
    }
}
