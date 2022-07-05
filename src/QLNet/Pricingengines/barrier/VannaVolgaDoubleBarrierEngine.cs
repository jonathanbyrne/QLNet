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
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Math.Distributions;
using QLNet.Math.Interpolations;
using QLNet.Processes;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.PricingEngines.barrier
{
    [PublicAPI]
    public class VannaVolgaDoubleBarrierEngine : GenericEngine<DoubleBarrierOption.Arguments, OneAssetOption.Results>
    {
        public delegate IPricingEngine GetOriginalEngine(GeneralizedBlackScholesProcess process, int series);

        private bool adaptVanDelta_;
        private Handle<DeltaVolQuote> atmVol_;
        private double bsPriceWithSmile_;
        private Handle<YieldTermStructure> domesticTS_;
        private Handle<YieldTermStructure> foreignTS_;
        private GetOriginalEngine getOriginalEngine_;
        private int series_;
        private Handle<Quote> spotFX_;
        private double T_;
        private Handle<DeltaVolQuote> vol25Call_;
        private Handle<DeltaVolQuote> vol25Put_;

        public VannaVolgaDoubleBarrierEngine(Handle<DeltaVolQuote> atmVol,
            Handle<DeltaVolQuote> vol25Put,
            Handle<DeltaVolQuote> vol25Call,
            Handle<Quote> spotFX,
            Handle<YieldTermStructure> domesticTS,
            Handle<YieldTermStructure> foreignTS,
            GetOriginalEngine getEngine,
            bool adaptVanDelta = false,
            double bsPriceWithSmile = 0.0,
            int series = 5)
        {
            atmVol_ = atmVol;
            vol25Put_ = vol25Put;
            vol25Call_ = vol25Call;
            T_ = atmVol_.link.maturity();
            spotFX_ = spotFX;
            domesticTS_ = domesticTS;
            foreignTS_ = foreignTS;
            adaptVanDelta_ = adaptVanDelta;
            bsPriceWithSmile_ = bsPriceWithSmile;
            series_ = series;
            getOriginalEngine_ = getEngine;

            QLNet.Utils.QL_REQUIRE(vol25Put_.link.delta().IsEqual(-0.25), () => "25 delta put is required by vanna volga method");
            QLNet.Utils.QL_REQUIRE(vol25Call_.link.delta().IsEqual(0.25), () => "25 delta call is required by vanna volga method");

            QLNet.Utils.QL_REQUIRE(vol25Put_.link.maturity().IsEqual(vol25Call_.link.maturity()) &&
                                   vol25Put_.link.maturity().IsEqual(atmVol_.link.maturity()), () =>
                "Maturity of 3 vols are not the same");

            QLNet.Utils.QL_REQUIRE(!domesticTS_.empty(), () => "domestic yield curve is not defined");
            QLNet.Utils.QL_REQUIRE(!foreignTS_.empty(), () => "foreign yield curve is not defined");

            atmVol_.registerWith(update);
            vol25Put_.registerWith(update);
            vol25Call_.registerWith(update);
            spotFX_.registerWith(update);
            domesticTS_.registerWith(update);
            foreignTS_.registerWith(update);
        }

        public override void calculate()
        {
            var sigmaShift_vega = 0.001;
            var sigmaShift_volga = 0.0001;
            var spotShift_delta = 0.0001 * spotFX_.link.value();
            var sigmaShift_vanna = 0.0001;

            QLNet.Utils.QL_REQUIRE(arguments_.barrierType == DoubleBarrier.Type.KnockIn ||
                                   arguments_.barrierType == DoubleBarrier.Type.KnockOut, () =>
                "Only same ExerciseType barrier supported");

            var x0Quote = new Handle<Quote>(new SimpleQuote(spotFX_.link.value()));
            var atmVolQuote = new Handle<Quote>(new SimpleQuote(atmVol_.link.value()));

            BlackVolTermStructure blackVolTS = new BlackConstantVol(Settings.evaluationDate(),
                new NullCalendar(), atmVolQuote, new Actual365Fixed());

            var stochProcess = new BlackScholesMertonProcess(x0Quote, foreignTS_, domesticTS_,
                new Handle<BlackVolTermStructure>(blackVolTS));

            var engineBS = getOriginalEngine_(stochProcess, series_);

            var blackDeltaCalculatorAtm = new BlackDeltaCalculator(
                QLNet.Option.Type.Call, atmVol_.link.deltaType(), x0Quote.link.value(),
                domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
                atmVol_.link.value() * System.Math.Sqrt(T_));

            var atmStrike = blackDeltaCalculatorAtm.atmStrike(atmVol_.link.atmType());

            var call25Vol = vol25Call_.link.value();
            var put25Vol = vol25Put_.link.value();
            var blackDeltaCalculatorPut25 = new BlackDeltaCalculator(
                QLNet.Option.Type.Put, vol25Put_.link.deltaType(), x0Quote.link.value(),
                domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
                put25Vol * System.Math.Sqrt(T_));
            var put25Strike = blackDeltaCalculatorPut25.strikeFromDelta(-0.25);
            var blackDeltaCalculatorCall25 = new BlackDeltaCalculator(
                QLNet.Option.Type.Call, vol25Call_.link.deltaType(), x0Quote.link.value(),
                domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
                call25Vol * System.Math.Sqrt(T_));
            var call25Strike = blackDeltaCalculatorCall25.strikeFromDelta(0.25);

            //here use vanna volga interpolated smile to price vanilla
            var strikes = new List<double>();
            var vols = new List<double>();
            strikes.Add(put25Strike);
            vols.Add(put25Vol);
            strikes.Add(atmStrike);
            vols.Add(atmVol_.link.value());
            strikes.Add(call25Strike);
            vols.Add(call25Vol);
            var vannaVolga = new VannaVolga(x0Quote.link.value(), foreignTS_.link.discount(T_),
                foreignTS_.link.discount(T_), T_);
            var interpolation = vannaVolga.interpolate(strikes, strikes.Count, vols);
            interpolation.enableExtrapolation();
            var payoff = arguments_.payoff as StrikedTypePayoff;
            QLNet.Utils.QL_REQUIRE(payoff != null, () => "invalid payoff");
            var strikeVol = interpolation.value(payoff.strike());
            // Vanilla option price
            var vanillaOption = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                strikeVol * System.Math.Sqrt(T_),
                domesticTS_.link.discount(T_));

            //already out
            if ((x0Quote.link.value() > arguments_.barrier_hi || x0Quote.link.value() < arguments_.barrier_lo)
                && arguments_.barrierType == DoubleBarrier.Type.KnockOut)
            {
                results_.value = 0.0;
                results_.additionalResults["VanillaPrice"] = adaptVanDelta_ ? bsPriceWithSmile_ : vanillaOption;
                results_.additionalResults["BarrierInPrice"] = adaptVanDelta_ ? bsPriceWithSmile_ : vanillaOption;
                results_.additionalResults["BarrierOutPrice"] = 0.0;
            }
            //already in
            else if ((x0Quote.link.value() > arguments_.barrier_hi || x0Quote.link.value() < arguments_.barrier_lo)
                     && arguments_.barrierType == DoubleBarrier.Type.KnockIn)
            {
                results_.value = adaptVanDelta_ ? bsPriceWithSmile_ : vanillaOption;
                results_.additionalResults["VanillaPrice"] = adaptVanDelta_ ? bsPriceWithSmile_ : vanillaOption;
                results_.additionalResults["BarrierInPrice"] = adaptVanDelta_ ? bsPriceWithSmile_ : vanillaOption;
                results_.additionalResults["BarrierOutPrice"] = 0.0;
            }
            else
            {
                //set up BS barrier option pricing
                //only calculate out barrier option price
                // in barrier price = vanilla - out barrier
                var doubleBarrierOption = new DoubleBarrierOption(
                    DoubleBarrier.Type.KnockOut,
                    arguments_.barrier_lo.GetValueOrDefault(),
                    arguments_.barrier_hi.GetValueOrDefault(),
                    arguments_.rebate.GetValueOrDefault(),
                    payoff,
                    arguments_.exercise);

                doubleBarrierOption.setPricingEngine(engineBS);

                //BS price
                var priceBS = doubleBarrierOption.NPV();

                var priceAtmCallBS = Utils.blackFormula(QLNet.Option.Type.Call, atmStrike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    atmVol_.link.value() * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));
                var price25CallBS = Utils.blackFormula(QLNet.Option.Type.Call, call25Strike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    atmVol_.link.value() * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));
                var price25PutBS = Utils.blackFormula(QLNet.Option.Type.Put, put25Strike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    atmVol_.link.value() * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));

                //market price
                var priceAtmCallMkt = Utils.blackFormula(QLNet.Option.Type.Call, atmStrike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    atmVol_.link.value() * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));
                var price25CallMkt = Utils.blackFormula(QLNet.Option.Type.Call, call25Strike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    call25Vol * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));
                var price25PutMkt = Utils.blackFormula(QLNet.Option.Type.Put, put25Strike,
                    x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_),
                    put25Vol * System.Math.Sqrt(T_),
                    domesticTS_.link.discount(T_));

                //Analytical Black Scholes formula
                var norm = new NormalDistribution();
                var d1atm = (System.Math.Log(x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_) / atmStrike)
                             + 0.5 * System.Math.Pow(atmVolQuote.link.value(), 2.0) * T_) / (atmVolQuote.link.value() * System.Math.Sqrt(T_));
                var vegaAtm_Analytical = x0Quote.link.value() * norm.value(d1atm) * System.Math.Sqrt(T_) * foreignTS_.link.discount(T_);
                var vannaAtm_Analytical = vegaAtm_Analytical / x0Quote.link.value() * (1.0 - d1atm / (atmVolQuote.link.value() * System.Math.Sqrt(T_)));
                var volgaAtm_Analytical = vegaAtm_Analytical * d1atm * (d1atm - atmVolQuote.link.value() * System.Math.Sqrt(T_)) / atmVolQuote.link.value();

                var d125call = (System.Math.Log(x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_) / call25Strike)
                                + 0.5 * System.Math.Pow(atmVolQuote.link.value(), 2.0) * T_) / (atmVolQuote.link.value() * System.Math.Sqrt(T_));
                var vega25Call_Analytical = x0Quote.link.value() * norm.value(d125call) * System.Math.Sqrt(T_) * foreignTS_.link.discount(T_);
                var vanna25Call_Analytical = vega25Call_Analytical / x0Quote.link.value() * (1.0 - d125call / (atmVolQuote.link.value() * System.Math.Sqrt(T_)));
                var volga25Call_Analytical = vega25Call_Analytical * d125call * (d125call - atmVolQuote.link.value() * System.Math.Sqrt(T_)) / atmVolQuote.link.value();

                var d125Put = (System.Math.Log(x0Quote.link.value() * foreignTS_.link.discount(T_) / domesticTS_.link.discount(T_) / put25Strike)
                               + 0.5 * System.Math.Pow(atmVolQuote.link.value(), 2.0) * T_) / (atmVolQuote.link.value() * System.Math.Sqrt(T_));
                var vega25Put_Analytical = x0Quote.link.value() * norm.value(d125Put) * System.Math.Sqrt(T_) * foreignTS_.link.discount(T_);
                var vanna25Put_Analytical = vega25Put_Analytical / x0Quote.link.value() * (1.0 - d125Put / (atmVolQuote.link.value() * System.Math.Sqrt(T_)));
                var volga25Put_Analytical = vega25Put_Analytical * d125Put * (d125Put - atmVolQuote.link.value() * System.Math.Sqrt(T_)) / atmVolQuote.link.value();

                //BS vega
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vega);
                doubleBarrierOption.recalculate();
                var vegaBarBS = (doubleBarrierOption.NPV() - priceBS) / sigmaShift_vega;
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() - sigmaShift_vega); //setback

                //BS volga

                //vegaBar2
                //base NPV
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_volga);
                doubleBarrierOption.recalculate();
                var priceBS2 = doubleBarrierOption.NPV();

                //shifted npv
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vega);
                doubleBarrierOption.recalculate();
                var vegaBarBS2 = (doubleBarrierOption.NPV() - priceBS2) / sigmaShift_vega;
                var volgaBarBS = (vegaBarBS2 - vegaBarBS) / sigmaShift_volga;
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value()
                                                                  - sigmaShift_volga
                                                                  - sigmaShift_vega); //setback

                //BS Delta
                //base delta
                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta); //shift forth
                doubleBarrierOption.recalculate();
                var priceBS_delta1 = doubleBarrierOption.NPV();

                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() - 2 * spotShift_delta); //shift back
                doubleBarrierOption.recalculate();
                var priceBS_delta2 = doubleBarrierOption.NPV();

                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta); //set back
                var deltaBar1 = (priceBS_delta1 - priceBS_delta2) / (2.0 * spotShift_delta);

                //shifted vanna
                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vanna); //shift sigma
                //shifted delta
                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta); //shift forth
                doubleBarrierOption.recalculate();
                priceBS_delta1 = doubleBarrierOption.NPV();

                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() - 2 * spotShift_delta); //shift back
                doubleBarrierOption.recalculate();
                priceBS_delta2 = doubleBarrierOption.NPV();

                ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta); //set back
                var deltaBar2 = (priceBS_delta1 - priceBS_delta2) / (2.0 * spotShift_delta);

                var vannaBarBS = (deltaBar2 - deltaBar1) / sigmaShift_vanna;

                ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() - sigmaShift_vanna); //set back

                //Matrix
                var A = new Matrix(3, 3, 0.0);

                //analytical
                A[0, 0] = vegaAtm_Analytical;
                A[0, 1] = vega25Call_Analytical;
                A[0, 2] = vega25Put_Analytical;
                A[1, 0] = vannaAtm_Analytical;
                A[1, 1] = vanna25Call_Analytical;
                A[1, 2] = vanna25Put_Analytical;
                A[2, 0] = volgaAtm_Analytical;
                A[2, 1] = volga25Call_Analytical;
                A[2, 2] = volga25Put_Analytical;

                var b = new Vector(3, 0.0);
                b[0] = vegaBarBS;
                b[1] = vannaBarBS;
                b[2] = volgaBarBS;
                var q = Matrix.inverse(A) * b;

                var H = arguments_.barrier_hi.GetValueOrDefault();
                var L = arguments_.barrier_lo.GetValueOrDefault();
                var theta_tilt_minus = ((domesticTS_.link.zeroRate(T_, Compounding.Continuous).value() -
                                         foreignTS_.link.zeroRate(T_, Compounding.Continuous).value()) /
                    atmVol_.link.value() - atmVol_.link.value() / 2.0) * System.Math.Sqrt(T_);
                var h = 1.0 / atmVol_.link.value() * System.Math.Log(H / x0Quote.link.value()) / System.Math.Sqrt(T_);
                var l = 1.0 / atmVol_.link.value() * System.Math.Log(L / x0Quote.link.value()) / System.Math.Sqrt(T_);
                var cnd = new CumulativeNormalDistribution();

                var doubleNoTouch = 0.0;
                for (var j = -series_; j < series_; j++)
                {
                    var e_minus = 2 * j * (h - l) - theta_tilt_minus;
                    doubleNoTouch += System.Math.Exp(-2.0 * j * theta_tilt_minus * (h - l)) * (cnd.value(h + e_minus) - cnd.value(l + e_minus))
                                     - System.Math.Exp(-2.0 * j * theta_tilt_minus * (h - l) + 2.0 * theta_tilt_minus * h) *
                                     (cnd.value(h - 2.0 * h + e_minus) - cnd.value(l - 2.0 * h + e_minus));
                }

                var p_survival = doubleNoTouch;

                var lambda = p_survival;
                var adjust = q[0] * (priceAtmCallMkt - priceAtmCallBS)
                             + q[1] * (price25CallMkt - price25CallBS)
                             + q[2] * (price25PutMkt - price25PutBS);
                var outPrice = priceBS + lambda * adjust; //
                double inPrice;

                //adapt Vanilla delta
                if (adaptVanDelta_)
                {
                    outPrice += lambda * (bsPriceWithSmile_ - vanillaOption);
                    //capfloored by (0, vanilla)
                    outPrice = System.Math.Max(0.0, System.Math.Min(bsPriceWithSmile_, outPrice));
                    inPrice = bsPriceWithSmile_ - outPrice;
                }
                else
                {
                    //capfloored by (0, vanilla)
                    outPrice = System.Math.Max(0.0, System.Math.Min(vanillaOption, outPrice));
                    inPrice = vanillaOption - outPrice;
                }

                if (arguments_.barrierType == DoubleBarrier.Type.KnockOut)
                {
                    results_.value = outPrice;
                }
                else
                {
                    results_.value = inPrice;
                }

                results_.additionalResults["VanillaPrice"] = vanillaOption;
                results_.additionalResults["BarrierInPrice"] = inPrice;
                results_.additionalResults["BarrierOutPrice"] = outPrice;
                results_.additionalResults["lambda"] = lambda;
            }
        }
    }
}
