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
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Models.Equity
{
    //! calibration helper for Heston model
    [PublicAPI]
    public class HestonModelHelper : CalibrationHelper
    {
        private Calendar calendar_;
        private Handle<YieldTermStructure> dividendYield_;
        private Date exerciseDate_;
        private Period maturity_;
        private VanillaOption option_;
        private Handle<Quote> s0_;
        private double strikePrice_;
        private double tau_;
        private Option.Type type_;

        public HestonModelHelper(Period maturity,
            Calendar calendar,
            double s0,
            double strikePrice,
            Handle<Quote> volatility,
            Handle<YieldTermStructure> riskFreeRate,
            Handle<YieldTermStructure> dividendYield,
            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError)
            : base(volatility, riskFreeRate, errorType)
        {
            maturity_ = maturity;
            calendar_ = calendar;
            s0_ = new Handle<Quote>(new SimpleQuote(s0));
            strikePrice_ = strikePrice;
            dividendYield_ = dividendYield;

            dividendYield.registerWith(update);
        }

        public HestonModelHelper(Period maturity,
            Calendar calendar,
            Handle<Quote> s0,
            double strikePrice,
            Handle<Quote> volatility,
            Handle<YieldTermStructure> riskFreeRate,
            Handle<YieldTermStructure> dividendYield,
            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError)
            : base(volatility, riskFreeRate, errorType)
        {
            maturity_ = maturity;
            calendar_ = calendar;
            s0_ = s0;
            strikePrice_ = strikePrice;
            dividendYield_ = dividendYield;

            s0.registerWith(update);
            dividendYield.registerWith(update);
        }

        public override void addTimesTo(List<double> t)
        {
        }

        public override double blackPrice(double volatility)
        {
            calculate();
            var stdDev = volatility * System.Math.Sqrt(maturity());
            return PricingEngines.Utils.blackFormula(type_, strikePrice_ * termStructure_.link.discount(tau_),
                s0_.link.value() * dividendYield_.link.discount(tau_), stdDev);
        }

        public double maturity()
        {
            calculate();
            return tau_;
        }

        public override double modelValue()
        {
            calculate();
            option_.setPricingEngine(engine_);
            return option_.NPV();
        }

        public Option.Type optionType()
        {
            calculate();
            return type_;
        }

        public double strike() => strikePrice_;

        protected override void performCalculations()
        {
            exerciseDate_ = calendar_.advance(termStructure_.link.referenceDate(), maturity_);
            tau_ = termStructure_.link.timeFromReference(exerciseDate_);
            type_ = strikePrice_ * termStructure_.link.discount(tau_) >=
                    s0_.link.value() * dividendYield_.link.discount(tau_)
                ? Option.Type.Call
                : Option.Type.Put;
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type_, strikePrice_);
            Exercise exercise = new EuropeanExercise(exerciseDate_);
            option_ = new VanillaOption(payoff, exercise);
            base.performCalculations();
        }
    }
}
