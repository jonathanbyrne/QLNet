﻿/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Pricingengines.CapFloor
{
    /// <summary>
    /// Black-formula cap/floor engine
    /// \ingroup capfloorengines
    /// </summary>
    public class BlackCapFloorEngine : CapFloorEngine
    {
        private Handle<YieldTermStructure> discountCurve_;
        private Handle<OptionletVolatilityStructure> vol_;
        private double displacement_;

        public BlackCapFloorEngine(Handle<YieldTermStructure> discountCurve, double vol,
                                   DayCounter dc = null, double displacement = 0.0)
        {
            discountCurve_ = discountCurve;
            vol_ = new Handle<OptionletVolatilityStructure>(new ConstantOptionletVolatility(0, new NullCalendar(), BusinessDayConvention.Following, vol, dc ?? new Actual365Fixed()));
            displacement_ = displacement;
            discountCurve_.registerWith(update);
        }
        public BlackCapFloorEngine(Handle<YieldTermStructure> discountCurve, Handle<Quote> vol,
                                   DayCounter dc = null, double displacement = 0.0)
        {
            discountCurve_ = discountCurve;
            vol_ = new Handle<OptionletVolatilityStructure>(new ConstantOptionletVolatility(
                                                               0, new NullCalendar(), BusinessDayConvention.Following, vol, dc ?? new Actual365Fixed()));
            displacement_ = displacement;
            discountCurve_.registerWith(update);
            vol_.registerWith(update);

        }
        public BlackCapFloorEngine(Handle<YieldTermStructure> discountCurve, Handle<OptionletVolatilityStructure> vol,
                                   double displacement = 0.0)
        {
            discountCurve_ = discountCurve;
            vol_ = vol;
            displacement_ = displacement;
            discountCurve_.registerWith(update);
            vol_.registerWith(update);
        }

        public override void calculate()
        {
            double value = 0.0;
            double vega = 0.0;
            int optionlets = arguments_.startDates.Count;
            List<double> values = new InitializedList<double>(optionlets);
            List<double> vegas = new InitializedList<double>(optionlets);
            List<double> stdDevs = new InitializedList<double>(optionlets);
            CapFloorType type = arguments_.type;
            Date today = vol_.link.referenceDate();
            Date settlement = discountCurve_.link.referenceDate();

            for (int i = 0; i < optionlets; ++i)
            {
                Date paymentDate = arguments_.endDates[i];
                if (paymentDate > settlement)
                {
                    // discard expired caplets
                    double d = arguments_.nominals[i] *
                               arguments_.gearings[i] *
                               discountCurve_.link.discount(paymentDate) *
                               arguments_.accrualTimes[i];

                    double? forward = arguments_.forwards[i];

                    Date fixingDate = arguments_.fixingDates[i];
                    double sqrtTime = 0.0;
                    if (fixingDate > today)
                        sqrtTime = System.Math.Sqrt(vol_.link.timeFromReference(fixingDate));

                    if (type == CapFloorType.Cap || type == CapFloorType.Collar)
                    {
                        double? strike = arguments_.capRates[i];
                        if (sqrtTime > 0.0)
                        {
                            stdDevs[i] = System.Math.Sqrt(vol_.link.blackVariance(fixingDate, strike.Value));
                            vegas[i] = Utils.blackFormulaStdDevDerivative(strike.Value, forward.Value, stdDevs[i], d, displacement_) * sqrtTime;
                        }
                        // include caplets with past fixing date
                        values[i] = Utils.blackFormula(QLNet.Option.Type.Call, strike.Value,
                                                       forward.Value, stdDevs[i], d, displacement_);
                    }
                    if (type == CapFloorType.Floor || type == CapFloorType.Collar)
                    {
                        double? strike = arguments_.floorRates[i];
                        double floorletVega = 0.0;

                        if (sqrtTime > 0.0)
                        {
                            stdDevs[i] = System.Math.Sqrt(vol_.link.blackVariance(fixingDate, strike.Value));
                            floorletVega = Utils.blackFormulaStdDevDerivative(strike.Value, forward.Value, stdDevs[i], d, displacement_) * sqrtTime;
                        }
                        double floorlet = Utils.blackFormula(QLNet.Option.Type.Put, strike.Value,
                                                             forward.Value, stdDevs[i], d, displacement_);
                        if (type == CapFloorType.Floor)
                        {
                            values[i] = floorlet;
                            vegas[i] = floorletVega;
                        }
                        else
                        {
                            // a collar is long a cap and short a floor
                            values[i] -= floorlet;
                            vegas[i] -= floorletVega;
                        }
                    }
                    value += values[i];
                    vega += vegas[i];
                }
            }
            results_.value = value;
            results_.additionalResults["vega"] = vega;

            results_.additionalResults["optionletsPrice"] = values;
            results_.additionalResults["optionletsVega"] = vegas;
            results_.additionalResults["optionletsAtmForward"] = arguments_.forwards;
            if (type != CapFloorType.Collar)
                results_.additionalResults["optionletsStdDev"] = stdDevs;
        }

        public Handle<YieldTermStructure> termStructure() { return discountCurve_; }
        public Handle<OptionletVolatilityStructure> volatility() { return vol_; }
        public double displacement() { return displacement_; }
    }
}
