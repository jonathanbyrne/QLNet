/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Patterns;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Models
{
    //! liquid market instrument used during calibration
    public abstract class CalibrationHelper : LazyObject
    {
        public enum CalibrationErrorType
        {
            RelativePriceError,
            PriceError,
            ImpliedVolError
        }

        private class ImpliedVolatilityHelper : ISolver1d
        {
            private readonly CalibrationHelper helper_;
            private readonly double value_;

            public ImpliedVolatilityHelper(CalibrationHelper helper, double value)
            {
                helper_ = helper;
                value_ = value;
            }

            public override double value(double x) => value_ - helper_.blackPrice(x);
        }

        protected IPricingEngine engine_;
        protected double marketValue_;
        protected double shift_;
        protected Handle<YieldTermStructure> termStructure_;
        protected Handle<Quote> volatility_;
        protected VolatilityType volatilityType_;
        private readonly CalibrationErrorType calibrationErrorType_;

        protected CalibrationHelper(Handle<Quote> volatility,
            Handle<YieldTermStructure> termStructure,
            CalibrationErrorType calibrationErrorType = CalibrationErrorType.RelativePriceError,
            VolatilityType type = VolatilityType.ShiftedLognormal,
            double shift = 0.0)
        {
            volatility_ = volatility;
            termStructure_ = termStructure;
            calibrationErrorType_ = calibrationErrorType;
            volatilityType_ = type;
            shift_ = shift;

            volatility_.registerWith(update);
            termStructure_.registerWith(update);
        }

        public abstract void addTimesTo(List<double> times);

        //! Black price given a volatility
        public abstract double blackPrice(double volatility);

        //! returns the price of the instrument according to the model
        public abstract double modelValue();

        //! returns the error resulting from the model valuation
        public virtual double calibrationError()
        {
            double error = 0;

            switch (calibrationErrorType_)
            {
                case CalibrationErrorType.RelativePriceError:
                    error = System.Math.Abs(marketValue() - modelValue()) / marketValue();
                    break;
                case CalibrationErrorType.PriceError:
                    error = marketValue() - modelValue();
                    break;
                case CalibrationErrorType.ImpliedVolError:
                {
                    var minVol = volatilityType_ == VolatilityType.ShiftedLognormal ? 0.0010 : 0.00005;
                    var maxVol = volatilityType_ == VolatilityType.ShiftedLognormal ? 10.0 : 0.50;
                    var lowerPrice = blackPrice(minVol);
                    var upperPrice = blackPrice(maxVol);
                    var modelPrice = modelValue();

                    double implied;
                    if (modelPrice <= lowerPrice)
                    {
                        implied = 0.001;
                    }
                    else if (modelPrice >= upperPrice)
                    {
                        implied = 10.0;
                    }
                    else
                    {
                        implied = impliedVolatility(modelPrice, 1e-12, 5000, 0.001, 10);
                    }

                    error = implied - volatility_.link.value();
                }
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown Calibration Error Type");
                    break;
            }

            return error;
        }

        //! Black volatility implied by the model
        public double impliedVolatility(double targetValue,
            double accuracy, int maxEvaluations, double minVol, double maxVol)
        {
            var f = new ImpliedVolatilityHelper(this, targetValue);
            var solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            return solver.solve(f, accuracy, volatility_.link.value(), minVol, maxVol);
        }

        //! returns the actual price of the instrument (from volatility)
        public double marketValue()
        {
            calculate();
            return marketValue_;
        }

        public void setPricingEngine(IPricingEngine engine)
        {
            engine_ = engine;
        }

        //! returns the volatility Handle
        public Handle<Quote> volatility() => volatility_;

        //! returns the volatility ExerciseType
        public VolatilityType volatilityType() => volatilityType_;

        protected override void performCalculations()
        {
            marketValue_ = blackPrice(volatility_.link.value());
        }
    }
}
