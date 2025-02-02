﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.PricingEngines.swaption;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Credit;
using QLNet.Time;

namespace QLNet.PricingEngines.Swap
{
    /*! Bilateral (CVA and DVA) default adjusted vanilla swap pricing
     engine. Collateral is not considered. No wrong way risk is
     considered (rates and counterparty default are uncorrelated).
     Based on:
     Sorensen,  E.H.  and  Bollier,  T.F.,  Pricing  swap  default
     risk. Financial Analysts Journal, 1994, 50, 23–33
     Also see sect. II-5 in: Risk Neutral Pricing of Counterparty Risk
     D. Brigo, M. Masetti, 2004
     or in sections 3 and 4 of "A Formula for Interest Rate Swaps
       Valuation under Counterparty Risk in presence of Netting Agreements"
     D. Brigo and M. Masetti; May 4, 2005

     to do: Compute fair rate through iteration instead of the
     current approximation .
     to do: write Issuer based constructors (event ExerciseType)
     to do: Check consistency between option engine discount and the one given
    */
    [PublicAPI]
    public class CounterpartyAdjSwapEngine : VanillaSwap.Engine
    {
        private Handle<IPricingEngine> baseSwapEngine_;
        private double ctptyRecoveryRate_;
        private Handle<DefaultProbabilityTermStructure> defaultTS_;
        private Handle<YieldTermStructure> discountCurve_;
        private Handle<DefaultProbabilityTermStructure> invstDTS_;
        private double invstRecoveryRate_;
        private Handle<IPricingEngine> swaptionletEngine_;

        // Constructors
        //!
        /*! Creates the engine from an arbitrary swaption engine.
          If the investor default model is not given a default
          free one is assumed.
          @param discountCurve Used in pricing.
          @param swaptionEngine Determines the volatility and thus the
          exposure model.
          @param ctptyDTS Counterparty default curve.
          @param ctptyRecoveryRate Counterparty recovey rate.
          @param invstDTS Investor (swap holder) default curve.
          @param invstRecoveryRate Investor recovery rate.
         */
        public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve,
            Handle<IPricingEngine> swaptionEngine, Handle<DefaultProbabilityTermStructure> ctptyDTS, double ctptyRecoveryRate,
            Handle<DefaultProbabilityTermStructure> invstDTS = null, double invstRecoveryRate = 0.999)
        {
            baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
            swaptionletEngine_ = swaptionEngine;
            discountCurve_ = discountCurve;
            defaultTS_ = ctptyDTS;
            ctptyRecoveryRate_ = ctptyRecoveryRate;
            invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
                new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0E-12, ctptyDTS.link.dayCounter()));
            invstRecoveryRate_ = invstRecoveryRate;

            discountCurve.registerWith(update);
            ctptyDTS.registerWith(update);
            invstDTS_.registerWith(update);
            swaptionEngine.registerWith(update);
        }

        /*! Creates an engine with a black volatility model for the
          exposure.
          If the investor default model is not given a default
          free one is assumed.
          @param discountCurve Used in pricing.
          @param blackVol Black volatility used in the exposure model.
          @param ctptyDTS Counterparty default curve.
          @param ctptyRecoveryRate Counterparty recovey rate.
          @param invstDTS Investor (swap holder) default curve.
          @param invstRecoveryRate Investor recovery rate.
         */
        public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve, double blackVol,
            Handle<DefaultProbabilityTermStructure> ctptyDTS, double ctptyRecoveryRate,
            Handle<DefaultProbabilityTermStructure> invstDTS = null, double invstRecoveryRate = 0.999)
        {
            baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
            swaptionletEngine_ = new Handle<IPricingEngine>(new BlackSwaptionEngine(discountCurve, blackVol));
            discountCurve_ = discountCurve;
            defaultTS_ = ctptyDTS;
            ctptyRecoveryRate_ = ctptyRecoveryRate;
            invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
                new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0e-12, ctptyDTS.link.dayCounter()));
            invstRecoveryRate_ = invstRecoveryRate;

            discountCurve.registerWith(update);
            ctptyDTS.registerWith(update);
            invstDTS_.registerWith(update);
        }

        /*! Creates an engine with a black volatility model for the
          exposure. The volatility is given as a quote.
          If the investor default model is not given a default
          free one is assumed.
          @param discountCurve Used in pricing.
          @param blackVol Black volatility used in the exposure model.
          @param ctptyDTS Counterparty default curve.
          @param ctptyRecoveryRate Counterparty recovey rate.
          @param invstDTS Investor (swap holder) default curve.
          @param invstRecoveryRate Investor recovery rate.
        */
        public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve, Handle<Quote> blackVol,
            Handle<DefaultProbabilityTermStructure> ctptyDTS, double ctptyRecoveryRate,
            Handle<DefaultProbabilityTermStructure> invstDTS = null, double invstRecoveryRate = 0.999)
        {
            baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
            swaptionletEngine_ = new Handle<IPricingEngine>(new BlackSwaptionEngine(discountCurve, blackVol));
            discountCurve_ = discountCurve;
            defaultTS_ = ctptyDTS;
            ctptyRecoveryRate_ = ctptyRecoveryRate;
            invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
                new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0e-12, ctptyDTS.link.dayCounter()));
            invstRecoveryRate_ = invstRecoveryRate;

            discountCurve.registerWith(update);
            ctptyDTS.registerWith(update);
            invstDTS_.registerWith(update);
            blackVol.registerWith(update);
        }

        public override void calculate()
        {
            /* both DTS, YTS ref dates and pricing date consistency
            checks? settlement... */
            QLNet.Utils.QL_REQUIRE(!discountCurve_.empty(), () => "no discount term structure set");
            QLNet.Utils.QL_REQUIRE(!defaultTS_.empty(), () => "no ctpty default term structure set");
            QLNet.Utils.QL_REQUIRE(!swaptionletEngine_.empty(), () => "no swap option engine set");

            var priceDate = defaultTS_.link.referenceDate();

            double cumOptVal = 0.0, cumPutVal = 0.0;
            // Vanilla swap so 0 leg is floater

            var index = 0;
            var nextFD = arguments_.fixedPayDates[index];
            var swapletStart = priceDate;
            while (nextFD < priceDate)
            {
                index++;
                nextFD = arguments_.fixedPayDates[index];
            }

            // Compute fair spread for strike value:
            // copy args into the non risky engine
            var noCVAArgs = baseSwapEngine_.link.getArguments() as Instruments.Swap.Arguments;

            noCVAArgs.legs = arguments_.legs;
            noCVAArgs.payer = arguments_.payer;

            baseSwapEngine_.link.calculate();

            var baseSwapRate = ((FixedRateCoupon)arguments_.legs[0][0]).rate();

            var vSResults = baseSwapEngine_.link.getResults() as Instruments.Swap.Results;

            var baseSwapFairRate = -baseSwapRate * vSResults.legNPV[1] / vSResults.legNPV[0];
            var baseSwapNPV = vSResults.value;

            var reversedType = arguments_.type == VanillaSwap.Type.Payer
                ? VanillaSwap.Type.Receiver
                : VanillaSwap.Type.Payer;

            // Swaplet options summatory:
            while (nextFD != arguments_.fixedPayDates.Last())
            {
                // iFD coupon not fixed, create swaptionlet:
                var swapIndex = ((FloatingRateCoupon)arguments_.legs[1][0]).index() as IborIndex;

                // Alternatively one could cap this period to, say, 1M
                var baseSwapsTenor = new Period(arguments_.fixedPayDates.Last().serialNumber()
                                                - swapletStart.serialNumber(), TimeUnit.Days);
                var swaplet = new MakeVanillaSwap(baseSwapsTenor, swapIndex, baseSwapFairRate)
                    .withType(arguments_.type)
                    .withNominal(arguments_.nominal)
                    .withEffectiveDate(swapletStart)
                    .withTerminationDate(arguments_.fixedPayDates.Last()).value();

                var revSwaplet = new MakeVanillaSwap(baseSwapsTenor, swapIndex, baseSwapFairRate)
                    .withType(reversedType)
                    .withNominal(arguments_.nominal)
                    .withEffectiveDate(swapletStart)
                    .withTerminationDate(arguments_.fixedPayDates.Last()).value();

                var swaptionlet = new Swaption(swaplet, new EuropeanExercise(swapletStart));
                var putSwaplet = new Swaption(revSwaplet, new EuropeanExercise(swapletStart));
                swaptionlet.setPricingEngine(swaptionletEngine_.currentLink());
                putSwaplet.setPricingEngine(swaptionletEngine_.currentLink());

                // atm underlying swap means that the value of put = value
                // call so this double pricing is not needed
                cumOptVal += swaptionlet.NPV() * defaultTS_.link.defaultProbability(
                    swapletStart, nextFD);
                cumPutVal += putSwaplet.NPV() * invstDTS_.link.defaultProbability(swapletStart, nextFD);

                swapletStart = nextFD;
                index++;
                nextFD = arguments_.fixedPayDates[index];
            }

            results_.value = baseSwapNPV - (1.0 - ctptyRecoveryRate_) * cumOptVal + (1.0 - invstRecoveryRate_) * cumPutVal;
            results_.fairRate = -baseSwapRate * (vSResults.legNPV[1] - (1.0 - ctptyRecoveryRate_) * cumOptVal +
                                                 (1.0 - invstRecoveryRate_) * cumPutVal) / vSResults.legNPV[0];
        }
    }
}
