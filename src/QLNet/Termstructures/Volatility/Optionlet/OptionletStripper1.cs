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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.PricingEngines.CapFloor;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.CapFloor;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.Optionlet
{
    using CapFloorMatrix = List<List<Instruments.CapFloor>>;

    /*! Helper class to strip optionlet (i.e. caplet/floorlet) volatilities
         (a.k.a. forward-forward volatilities) from the (cap/floor) term
         volatilities of a CapFloorTermVolSurface.
     */
    [PublicAPI]
    public class OptionletStripper1 : OptionletStripper
    {
        private double accuracy_;
        private bool capFlooMatrixNotInitialized_;
        private Matrix capFloorPrices_, optionletPrices_;
        private CapFloorMatrix capFloors_;
        private Matrix capFloorVols_;
        private bool dontThrow_;
        private bool floatingSwitchStrike_;
        private int maxIter_;
        private Matrix optionletStDevs_;
        private double? switchStrike_;
        private List<List<SimpleQuote>> volQuotes_;

        public OptionletStripper1(CapFloorTermVolSurface termVolSurface, IborIndex index,
            double? switchStrike = null,
            double accuracy = 1.0e-6,
            int maxIter = 100,
            Handle<YieldTermStructure> discount = null,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double displacement = 0.0,
            bool dontThrow = false)
            : base(termVolSurface, index, discount, type, displacement)
        {
            volQuotes_ = new InitializedList<List<SimpleQuote>>(nOptionletTenors_,
                new InitializedList<SimpleQuote>(nStrikes_, new SimpleQuote()));
            floatingSwitchStrike_ = switchStrike == null;
            capFlooMatrixNotInitialized_ = true;
            switchStrike_ = switchStrike;
            accuracy_ = accuracy;
            maxIter_ = maxIter;
            dontThrow_ = dontThrow;

            capFloorPrices_ = new Matrix(nOptionletTenors_, nStrikes_);
            optionletPrices_ = new Matrix(nOptionletTenors_, nStrikes_);
            capFloorVols_ = new Matrix(nOptionletTenors_, nStrikes_);
            var firstGuess = 0.14; // guess is only used for shifted lognormal vols
            optionletStDevs_ = new Matrix(nOptionletTenors_, nStrikes_, firstGuess);

            capFloors_ = new InitializedList<List<Instruments.CapFloor>>(nOptionletTenors_);
        }

        public Matrix capFloorPrices()
        {
            calculate();
            return capFloorPrices_;
        }

        public Matrix capFloorVolatilities()
        {
            calculate();
            return capFloorVols_;
        }

        public Matrix optionletPrices()
        {
            calculate();
            return optionletPrices_;
        }

        public double switchStrike()
        {
            if (floatingSwitchStrike_)
            {
                calculate();
            }

            return switchStrike_.Value;
        }

        // LazyObject interface
        protected override void performCalculations()
        {
            // update dates
            var referenceDate = termVolSurface_.referenceDate();
            var dc = termVolSurface_.dayCounter();
            var dummy = new BlackCapFloorEngine( // discounting does not matter here
                iborIndex_.forwardingTermStructure(), 0.20, dc);
            for (var i = 0; i < nOptionletTenors_; ++i)
            {
                Instruments.CapFloor temp = new MakeCapFloor(CapFloorType.Cap,
                        capFloorLengths_[i],
                        iborIndex_,
                        0.04, // dummy strike
                        new Period(0, TimeUnit.Days))
                    .withPricingEngine(dummy);
                var lFRC = temp.lastFloatingRateCoupon();
                optionletDates_[i] = lFRC.fixingDate();
                optionletPaymentDates_[i] = lFRC.date();
                optionletAccrualPeriods_[i] = lFRC.accrualPeriod();
                optionletTimes_[i] = dc.yearFraction(referenceDate,
                    optionletDates_[i]);
                atmOptionletRate_[i] = lFRC.indexFixing();
            }

            if (floatingSwitchStrike_ && capFlooMatrixNotInitialized_)
            {
                var averageAtmOptionletRate = 0.0;
                for (var i = 0; i < nOptionletTenors_; ++i)
                {
                    averageAtmOptionletRate += atmOptionletRate_[i];
                }

                switchStrike_ = averageAtmOptionletRate / nOptionletTenors_;
            }

            var discountCurve = discount_.empty()
                ? iborIndex_.forwardingTermStructure()
                : discount_;

            var strikes = new List<double>(termVolSurface_.strikes());
            // initialize CapFloorMatrix
            if (capFlooMatrixNotInitialized_)
            {
                for (var i = 0; i < nOptionletTenors_; ++i)
                {
                    capFloors_[i] = new List<Instruments.CapFloor>(nStrikes_);
                }

                // construction might go here
                for (var j = 0; j < nStrikes_; ++j)
                {
                    // using out-of-the-money options
                    var capFloorType = strikes[j] < switchStrike_
                        ? CapFloorType.Floor
                        : CapFloorType.Cap;
                    for (var i = 0; i < nOptionletTenors_; ++i)
                    {
                        if (volatilityType_ == VolatilityType.ShiftedLognormal)
                        {
                            var engine = new BlackCapFloorEngine(discountCurve,
                                new Handle<Quote>(volQuotes_[i][j]), dc, displacement_);
                            capFloors_[i].Add(new MakeCapFloor(capFloorType, capFloorLengths_[i], iborIndex_, strikes[j],
                                new Period(0, TimeUnit.Days)).withPricingEngine(engine));
                        }
                        else if (volatilityType_ == VolatilityType.Normal)
                        {
                            var engine = new BachelierCapFloorEngine(discountCurve,
                                new Handle<Quote>(volQuotes_[i][j]), dc);
                            capFloors_[i].Add(new MakeCapFloor(capFloorType, capFloorLengths_[i], iborIndex_, strikes[j],
                                new Period(0, TimeUnit.Days)).withPricingEngine(engine));
                        }
                        else
                        {
                            QLNet.Utils.QL_FAIL("unknown volatility ExerciseType: " + volatilityType_);
                        }
                    }
                }

                capFlooMatrixNotInitialized_ = false;
            }

            for (var j = 0; j < nStrikes_; ++j)
            {
                var optionletType = strikes[j] < switchStrike_ ? Option.Type.Put : Option.Type.Call;

                var previousCapFloorPrice = 0.0;
                for (var i = 0; i < nOptionletTenors_; ++i)
                {
                    capFloorVols_[i, j] = termVolSurface_.volatility(capFloorLengths_[i], strikes[j], true);
                    volQuotes_[i][j].setValue(capFloorVols_[i, j]);

                    capFloorPrices_[i, j] = capFloors_[i][j].NPV();
                    optionletPrices_[i, j] = capFloorPrices_[i, j] - previousCapFloorPrice;
                    previousCapFloorPrice = capFloorPrices_[i, j];
                    var d = discountCurve.link.discount(optionletPaymentDates_[i]);
                    var optionletAnnuity = optionletAccrualPeriods_[i] * d;
                    try
                    {
                        if (volatilityType_ == VolatilityType.ShiftedLognormal)
                        {
                            optionletStDevs_[i, j] = PricingEngines.Utils.blackFormulaImpliedStdDev(optionletType, strikes[j], atmOptionletRate_[i],
                                optionletPrices_[i, j], optionletAnnuity, displacement_, optionletStDevs_[i, j], accuracy_,
                                maxIter_);
                        }
                        else if (volatilityType_ == VolatilityType.Normal)
                        {
                            optionletStDevs_[i, j] = System.Math.Sqrt(optionletTimes_[i]) *
                                                     PricingEngines.Utils.bachelierBlackFormulaImpliedVol(
                                                         optionletType, strikes[j], atmOptionletRate_[i],
                                                         optionletTimes_[i], optionletPrices_[i, j],
                                                         optionletAnnuity);
                        }
                        else
                        {
                            QLNet.Utils.QL_FAIL("Unknown volatility ExerciseType: " + volatilityType_);
                        }
                    }
                    catch (Exception e)
                    {
                        if (dontThrow_)
                        {
                            optionletStDevs_[i, j] = 0.0;
                        }
                        else
                        {
                            QLNet.Utils.QL_FAIL("could not bootstrap optionlet:" +
                                                         "\n ExerciseType:    " + optionletType +
                                                         "\n strike:  " + strikes[j] +
                                                         "\n atm:     " + atmOptionletRate_[i] +
                                                         "\n price:   " + optionletPrices_[i, j] +
                                                         "\n annuity: " + optionletAnnuity +
                                                         "\n expiry:  " + optionletDates_[i] +
                                                         "\n error:   " + e.Message);
                        }
                    }

                    optionletVolatilities_[i][j] = optionletStDevs_[i, j] / System.Math.Sqrt(optionletTimes_[i]);
                }
            }
        }
    }
}
