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
using QLNet.Patterns;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Time.DayCounters;

namespace QLNet.Termstructures.Volatility
{
    //! interest rate volatility smile section
    /*! This abstract class provides volatility smile section interface */
    public abstract class SmileSection : LazyObject
    {
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
                referenceDate_ = referenceDate;
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

        protected SmileSection() { }


        public override void update()
        {
            if (isFloating_)
            {
                referenceDate_ = Settings.evaluationDate();
                initializeExerciseTime();
            }
        }
        public abstract double minStrike();
        public abstract double maxStrike();
        public double variance(double strike) => varianceImpl(strike);

        public double volatility(double strike) => volatilityImpl(strike);

        public abstract double? atmLevel();
        public virtual Date exerciseDate() => exerciseDate_;

        public virtual VolatilityType volatilityType() => volatilityType_;

        public virtual double shift() => shift_;

        public virtual Date referenceDate()
        {
            Utils.QL_REQUIRE(referenceDate_ != null, () => "referenceDate not available for this instance");
            return referenceDate_;
        }
        public virtual double exerciseTime() => exerciseTime_;

        public virtual DayCounter dayCounter() => dc_;

        public virtual double optionPrice(double strike, QLNet.Option.Type type = QLNet.Option.Type.Call, double discount = 1.0)
        {
            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () => "smile section must provide atm level to compute option price");
            // if lognormal or shifted lognormal,
            // for strike at -shift, return option price even if outside
            // minstrike, maxstrike interval
            if (volatilityType() == VolatilityType.ShiftedLognormal)
                return Utils.blackFormula(type, strike, atm.Value, System.Math.Abs(strike + shift()) < Const.QL_EPSILON ?
                                          0.2 : System.Math.Sqrt(variance(strike)), discount, shift());
            else
                return Utils.bachelierBlackFormula(type, strike, atm.Value, System.Math.Sqrt(variance(strike)), discount);
        }
        public virtual double digitalOptionPrice(double strike, QLNet.Option.Type type = QLNet.Option.Type.Call, double discount = 1.0,
                                                 double gap = 1.0e-5)
        {
            var m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
            var kl = System.Math.Max(strike - gap / 2.0, m);
            var kr = kl + gap;
            return (type == QLNet.Option.Type.Call ? 1.0 : -1.0) *
                   (optionPrice(kl, type, discount) - optionPrice(kr, type, discount)) / gap;
        }
        public virtual double vega(double strike, double discount = 1.0)
        {
            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () =>
                             "smile section must provide atm level to compute option vega");
            if (volatilityType() == VolatilityType.ShiftedLognormal)
                return Utils.blackFormulaVolDerivative(strike, atmLevel().Value,
                                                       System.Math.Sqrt(variance(strike)),
                                                       exerciseTime(), discount, shift()) * 0.01;
            else
                Utils.QL_FAIL("vega for normal smilesection not yet implemented");

            return 0;

        }
        public virtual double density(double strike, double discount = 1.0, double gap = 1.0E-4)
        {
            var m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
            var kl = System.Math.Max(strike - gap / 2.0, m);
            var kr = kl + gap;
            return (digitalOptionPrice(kl, QLNet.Option.Type.Call, discount, gap) -
                    digitalOptionPrice(kr, QLNet.Option.Type.Call, discount, gap)) / gap;
        }
        public double volatility(double strike, VolatilityType volatilityType, double shift = 0.0)
        {

            if (volatilityType == volatilityType_ && Utils.close(shift, this.shift()))
                return volatility(strike);
            var atm = atmLevel();
            Utils.QL_REQUIRE(atm != null, () => "smile section must provide atm level to compute converted volatilties");
            var type = strike >= atm ? QLNet.Option.Type.Call : QLNet.Option.Type.Put;
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
            else
            {
                return Utils.bachelierBlackFormulaImpliedVol(type, strike, atm.Value, exerciseTime(), premium);
            }
        }

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
        protected abstract double volatilityImpl(double strike);


        private bool isFloating_;
        private Date referenceDate_;
        private Date exerciseDate_;
        private DayCounter dc_;
        private double exerciseTime_;
        private VolatilityType volatilityType_;
        private double shift_;
    }
    [JetBrains.Annotations.PublicAPI] public class SabrSmileSection : SmileSection
    {
        private double alpha_, beta_, nu_, rho_, forward_, shift_;
        private VolatilityType volatilityType_;

        public SabrSmileSection(double timeToExpiry, double forward, List<double> sabrParams, VolatilityType volatilityType = VolatilityType.ShiftedLognormal, double shift = 0.0)
           : base(timeToExpiry, null, volatilityType, shift)
        {
            forward_ = forward;
            shift_ = shift;
            volatilityType_ = volatilityType;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            Utils.QL_REQUIRE(volatilityType == VolatilityType.Normal || forward_ + shift_ > 0.0, () => "at the money forward rate + shift must be: " + forward_ + shift_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public SabrSmileSection(Date d, double forward, List<double> sabrParams, DayCounter dc = null, VolatilityType volatilityType = VolatilityType.ShiftedLognormal, double shift = 0.0)
           : base(d, dc ?? new Actual365Fixed(), null, volatilityType, shift)
        {
            forward_ = forward;
            shift_ = shift;
            volatilityType_ = volatilityType;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            Utils.QL_REQUIRE(volatilityType == VolatilityType.Normal || forward_ + shift_ > 0.0, () => "at the money forward rate +shift must be: " + forward_ + shift_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public override double minStrike() => 0.0;

        public override double maxStrike() => double.MaxValue;

        public override double? atmLevel() => forward_;

        protected override double varianceImpl(double strike)
        {
            double vol;
            if (volatilityType_ == VolatilityType.ShiftedLognormal)
                vol = Utils.shiftedSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);
            else
                vol = Utils.shiftedSabrNormalVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);

            return vol * vol * exerciseTime();
        }

        protected override double volatilityImpl(double strike)
        {
            double vol;
            if (volatilityType_ == VolatilityType.ShiftedLognormal)
                vol = Utils.shiftedSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);
            else
                vol = Utils.shiftedSabrNormalVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_, shift_);

            return vol;
        }
    }
}
