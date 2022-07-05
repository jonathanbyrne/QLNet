/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using QLNet.Patterns;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility
{
    //! interest rate volatility smile section
    /*! This abstract class provides volatility smile section interface */
    public abstract class SmileSection : LazyObject
    {
        private readonly DayCounter dc_;
        private readonly Date exerciseDate_;
        private double exerciseTime_;
        private readonly bool isFloating_;
        private Date referenceDate_;
        private readonly double shift_;
        private readonly VolatilityType volatilityType_;

        protected SmileSection(Date d, DayCounter dc = null, Date referenceDate = null,
            VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0)
        {
            exerciseDate_ = d;
            dc_ = dc;
            volatilityType_ = type;
            shift_ = shift;

            isFloating_ = referenceDate == null;
            if (isFloating_)
            {
                Settings.registerWith(update);
                referenceDate_ = Settings.evaluationDate();
            }
            else
            {
                referenceDate_ = referenceDate;
            }

            initializeExerciseTime();
        }

        protected SmileSection(double exerciseTime, DayCounter dc = null,
            VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0)
        {
            isFloating_ = false;
            referenceDate_ = null;
            dc_ = dc;
            exerciseTime_ = exerciseTime;
            volatilityType_ = type;
            shift_ = shift;

            Utils.QL_REQUIRE(exerciseTime_ >= 0.0, () => "expiry time must be positive: " + exerciseTime_ + " not allowed");
        }

        protected SmileSection()
        {
        }

        public abstract double? atmLevel();

        public abstract double maxStrike();

        public abstract double minStrike();

        public virtual DayCounter dayCounter() => dc_;

        public virtual double density(double strike, double discount = 1.0, double gap = 1.0E-4)
        {
            var m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
            var kl = System.Math.Max(strike - gap / 2.0, m);
            var kr = kl + gap;
            return (digitalOptionPrice(kl, Option.Type.Call, discount, gap) -
                    digitalOptionPrice(kr, Option.Type.Call, discount, gap)) / gap;
        }

        public virtual double digitalOptionPrice(double strike, Option.Type type = Option.Type.Call, double discount = 1.0,
            double gap = 1.0e-5)
        {
            var m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
            var kl = System.Math.Max(strike - gap / 2.0, m);
            var kr = kl + gap;
            return (type == Option.Type.Call ? 1.0 : -1.0) *
                (optionPrice(kl, type, discount) - optionPrice(kr, type, discount)) / gap;
        }

        public virtual Date exerciseDate() => exerciseDate_;

        public virtual double exerciseTime() => exerciseTime_;

        public virtual double optionPrice(double strike, Option.Type type = Option.Type.Call, double discount = 1.0)
        {
            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () => "smile section must provide atm level to compute option price");
            // if lognormal or shifted lognormal,
            // for strike at -shift, return option price even if outside
            // minstrike, maxstrike interval
            if (volatilityType() == VolatilityType.ShiftedLognormal)
            {
                return Utils.blackFormula(type, strike, atm.Value, System.Math.Abs(strike + shift()) < Const.QL_EPSILON ? 0.2 : System.Math.Sqrt(variance(strike)), discount, shift());
            }

            return Utils.bachelierBlackFormula(type, strike, atm.Value, System.Math.Sqrt(variance(strike)), discount);
        }

        public virtual Date referenceDate()
        {
            Utils.QL_REQUIRE(referenceDate_ != null, () => "referenceDate not available for this instance");
            return referenceDate_;
        }

        public virtual double shift() => shift_;

        public override void update()
        {
            if (isFloating_)
            {
                referenceDate_ = Settings.evaluationDate();
                initializeExerciseTime();
            }
        }

        public double variance(double strike) => varianceImpl(strike);

        public virtual double vega(double strike, double discount = 1.0)
        {
            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () =>
                "smile section must provide atm level to compute option vega");
            if (volatilityType() == VolatilityType.ShiftedLognormal)
            {
                return Utils.blackFormulaVolDerivative(strike, atmLevel().Value,
                    System.Math.Sqrt(variance(strike)),
                    exerciseTime(), discount, shift()) * 0.01;
            }

            Utils.QL_FAIL("vega for normal smilesection not yet implemented");

            return 0;
        }

        public double volatility(double strike) => volatilityImpl(strike);

        public double volatility(double strike, VolatilityType volatilityType, double shift = 0.0)
        {
            if (volatilityType == volatilityType_ && Utils.close(shift, this.shift()))
            {
                return volatility(strike);
            }

            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () => "smile section must provide atm level to compute converted volatilties");
            var type = strike >= atm ? Option.Type.Call : Option.Type.Put;
            var premium = optionPrice(strike, type);
            var premiumAtm = optionPrice(atm.Value, type);
            if (volatilityType == VolatilityType.ShiftedLognormal)
            {
                try
                {
                    return Utils.blackFormulaImpliedStdDev(type, strike, atm.Value, premium, 1.0, shift) /
                           System.Math.Sqrt(exerciseTime());
                }
                catch (Exception)
                {
                    return Utils.blackFormulaImpliedStdDevChambers(type, strike, atm.Value, premium, premiumAtm, 1.0, shift) /
                           System.Math.Sqrt(exerciseTime());
                }
            }

            return Utils.bachelierBlackFormulaImpliedVol(type, strike, atm.Value, exerciseTime(), premium);
        }

        public virtual VolatilityType volatilityType() => volatilityType_;

        protected abstract double volatilityImpl(double strike);

        protected virtual void initializeExerciseTime()
        {
            Utils.QL_REQUIRE(exerciseDate_ >= referenceDate_, () =>
                "expiry date (" + exerciseDate_ +
                ") must be greater than reference date (" +
                referenceDate_ + ")");
            exerciseTime_ = dc_.yearFraction(referenceDate_, exerciseDate_);
        }

        protected virtual double varianceImpl(double strike)
        {
            var v = volatilityImpl(strike);
            return v * v * exerciseTime();
        }
    }
}
