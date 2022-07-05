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

using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.swaption
{
    public abstract class SwaptionVolatilityCube : SwaptionVolatilityDiscrete
    {
        protected Handle<SwaptionVolatilityStructure> atmVol_;
        protected List<double> localSmile_;
        protected List<double> localStrikes_;
        protected int nStrikes_;
        protected List<double> strikeSpreads_;
        protected SwapIndex swapIndexBase_, shortSwapIndexBase_;
        protected bool vegaWeightedSmileFit_;
        protected List<List<Handle<Quote>>> volSpreads_;

        protected SwaptionVolatilityCube(Handle<SwaptionVolatilityStructure> atmVol,
            List<Period> optionTenors,
            List<Period> swapTenors,
            List<double> strikeSpreads,
            List<List<Handle<Quote>>> volSpreads,
            SwapIndex swapIndexBase,
            SwapIndex shortSwapIndexBase,
            bool vegaWeightedSmileFit)
            : base(optionTenors, swapTenors, 0, atmVol.link.calendar(), atmVol.link.businessDayConvention(),
                atmVol.link.dayCounter())
        {
            atmVol_ = atmVol;
            nStrikes_ = strikeSpreads.Count;
            strikeSpreads_ = strikeSpreads;
            localStrikes_ = new InitializedList<double>(nStrikes_);
            localSmile_ = new List<double>(nStrikes_);
            volSpreads_ = volSpreads;
            swapIndexBase_ = swapIndexBase;
            shortSwapIndexBase_ = shortSwapIndexBase;
            vegaWeightedSmileFit_ = vegaWeightedSmileFit;

            QLNet.Utils.QL_REQUIRE(!atmVol_.empty(), () => "atm vol handle not linked to anything");
            for (var i = 1; i < nStrikes_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(strikeSpreads_[i - 1] < strikeSpreads_[i], () =>
                    "non increasing strike spreads: " + i + " is " + strikeSpreads_[i - 1] + ", " +
                    (i + 1) + " is " + strikeSpreads_[i]);
            }

            QLNet.Utils.QL_REQUIRE(!volSpreads_.empty(), () => "empty vol spreads matrix");

            QLNet.Utils.QL_REQUIRE(nOptionTenors_ * nSwapTenors_ == volSpreads_.Count, () =>
                "mismatch between number of option tenors * swap tenors (" +
                nOptionTenors_ * nSwapTenors_ + ") and number of rows (" +
                volSpreads_.Count + ")");

            for (var i = 0; i < volSpreads_.Count; i++)
            {
                QLNet.Utils.QL_REQUIRE(nStrikes_ == volSpreads_[i].Count, () =>
                    "mismatch between number of strikes (" + nStrikes_ +
                    ") and number of columns (" + volSpreads_[i].Count +
                    ") in the " + (i + 1) + " row");
            }

            atmVol_.registerWith(update);
            atmVol_.link.enableExtrapolation();

            swapIndexBase_.registerWith(update);
            shortSwapIndexBase_.registerWith(update);

            QLNet.Utils.QL_REQUIRE(shortSwapIndexBase_.tenor() < swapIndexBase_.tenor(), () =>
                "short index tenor (" + shortSwapIndexBase_.tenor() +
                ") is not less than index tenor (" +
                swapIndexBase_.tenor() + ")");

            registerWithVolatilitySpread();
            Settings.registerWith(update);
            evaluationDate_ = Settings.evaluationDate();
        }

        // Other inspectors
        public double atmStrike(Date optionDate, Period swapTenor)
        {
            // FIXME use a familyName-based index factory
            if (swapTenor > shortSwapIndexBase_.tenor())
            {
                if (swapIndexBase_.exogenousDiscount())
                {
                    return new SwapIndex(swapIndexBase_.familyName(),
                        swapTenor,
                        swapIndexBase_.fixingDays(),
                        swapIndexBase_.currency(),
                        swapIndexBase_.fixingCalendar(),
                        swapIndexBase_.fixedLegTenor(),
                        swapIndexBase_.fixedLegConvention(),
                        swapIndexBase_.dayCounter(),
                        swapIndexBase_.iborIndex(),
                        swapIndexBase_.discountingTermStructure()).fixing(optionDate);
                }

                return new SwapIndex(swapIndexBase_.familyName(),
                    swapTenor,
                    swapIndexBase_.fixingDays(),
                    swapIndexBase_.currency(),
                    swapIndexBase_.fixingCalendar(),
                    swapIndexBase_.fixedLegTenor(),
                    swapIndexBase_.fixedLegConvention(),
                    swapIndexBase_.dayCounter(),
                    swapIndexBase_.iborIndex()).fixing(optionDate);
            }

            if (shortSwapIndexBase_.exogenousDiscount())
            {
                return new SwapIndex(shortSwapIndexBase_.familyName(),
                    swapTenor,
                    shortSwapIndexBase_.fixingDays(),
                    shortSwapIndexBase_.currency(),
                    shortSwapIndexBase_.fixingCalendar(),
                    shortSwapIndexBase_.fixedLegTenor(),
                    shortSwapIndexBase_.fixedLegConvention(),
                    shortSwapIndexBase_.dayCounter(),
                    shortSwapIndexBase_.iborIndex(),
                    shortSwapIndexBase_.discountingTermStructure()).fixing(optionDate);
            }

            return new SwapIndex(shortSwapIndexBase_.familyName(),
                swapTenor,
                shortSwapIndexBase_.fixingDays(),
                shortSwapIndexBase_.currency(),
                shortSwapIndexBase_.fixingCalendar(),
                shortSwapIndexBase_.fixedLegTenor(),
                shortSwapIndexBase_.fixedLegConvention(),
                shortSwapIndexBase_.dayCounter(),
                shortSwapIndexBase_.iborIndex()).fixing(optionDate);
        }

        public double atmStrike(Period optionTenor, Period swapTenor)
        {
            var optionDate = optionDateFromTenor(optionTenor);
            return atmStrike(optionDate, swapTenor);
        }

        public Handle<SwaptionVolatilityStructure> atmVol() => atmVol_;

        public new Calendar calendar() => atmVol_.link.calendar();

        // TermStructure interface
        public new DayCounter dayCounter() => atmVol_.link.dayCounter();

        public override Date maxDate() => atmVol_.link.maxDate();

        public override double maxStrike() => double.MaxValue;

        // SwaptionVolatilityStructure interface
        public override Period maxSwapTenor() => atmVol_.link.maxSwapTenor();

        public new double maxTime() => atmVol_.link.maxTime();

        // VolatilityTermStructure interface
        public override double minStrike() => -double.MaxValue;

        public override Date referenceDate()
        {
            if (atmVol_ == null)
            {
                return base.referenceDate();
            }

            return atmVol_.link.referenceDate();
        }

        public new int settlementDays() => atmVol_.link.settlementDays();

        public SwapIndex shortSwapIndexBase() => shortSwapIndexBase_;

        public List<double> strikeSpreads() => strikeSpreads_;

        public SwapIndex swapIndexBase() => swapIndexBase_;

        public bool vegaWeightedSmileFit() => vegaWeightedSmileFit_;

        public override VolatilityType volatilityType() => atmVol_.link.volatilityType();

        public List<List<Handle<Quote>>> volSpreads() => volSpreads_;

        // LazyObject interface
        protected override void performCalculations()
        {
            QLNet.Utils.QL_REQUIRE(nStrikes_ >= requiredNumberOfStrikes(), () =>
                "too few strikes (" + nStrikes_
                                    + ") required are at least "
                                    + requiredNumberOfStrikes());
            base.performCalculations();
        }

        protected void registerWithVolatilitySpread()
        {
            for (var i = 0; i < nStrikes_; i++)
            for (var j = 0; j < nOptionTenors_; j++)
            for (var k = 0; k < nSwapTenors_; k++)
            {
                volSpreads_[j * nSwapTenors_ + k][i].registerWith(update);
            }
        }

        protected virtual int requiredNumberOfStrikes() => 2;

        protected override double shiftImpl(double optionTime, double swapLength) => atmVol_.link.shift(optionTime, swapLength);

        protected override double volatilityImpl(double optionTime, double swapLength, double strike) => smileSectionImpl(optionTime, swapLength).volatility(strike);

        protected override double volatilityImpl(Date optionDate, Period swapTenor, double strike) => smileSectionImpl(optionDate, swapTenor).volatility(strike);
    }
}
