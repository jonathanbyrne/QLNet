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
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Math.Distributions;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility;
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class RangeAccrualFloatersCoupon : FloatingRateCoupon
    {
        public RangeAccrualFloatersCoupon(Date paymentDate,
                                          double nominal,
                                          IborIndex index,
                                          Date startDate,
                                          Date endDate,
                                          int fixingDays,
                                          DayCounter dayCounter,
                                          double gearing,
                                          double spread,
                                          Date refPeriodStart,
                                          Date refPeriodEnd,
                                          Schedule observationsSchedule,
                                          double lowerTrigger,
                                          double upperTrigger)
           : base(paymentDate, nominal, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd,
                  dayCounter)
        {
            observationsSchedule_ = observationsSchedule;
            lowerTrigger_ = lowerTrigger;
            upperTrigger_ = upperTrigger;

            Utils.QL_REQUIRE(lowerTrigger_ < upperTrigger, () => "lowerTrigger_>=upperTrigger");
            Utils.QL_REQUIRE(observationsSchedule_.startDate() == startDate, () => "incompatible start date");
            Utils.QL_REQUIRE(observationsSchedule_.endDate() == endDate, () => "incompatible end date");

            observationDates_ = new List<Date>(observationsSchedule_.dates());
            observationDates_.RemoveAt(observationDates_.Count - 1); //remove end date
            observationDates_.RemoveAt(0);                         //remove start date
            observationsNo_ = observationDates_.Count;

            var rateCurve = index.forwardingTermStructure();
            var referenceDate = rateCurve.link.referenceDate();

            startTime_ = dayCounter.yearFraction(referenceDate, startDate);
            endTime_ = dayCounter.yearFraction(referenceDate, endDate);
            observationTimes_ = new List<double>();
            for (var i = 0; i < observationsNo_; i++)
            {
                observationTimes_.Add(dayCounter.yearFraction(referenceDate, observationDates_[i]));
            }
        }

        public double startTime() => startTime_;

        public double endTime() => endTime_;

        public double lowerTrigger() => lowerTrigger_;

        public double upperTrigger() => upperTrigger_;

        public int observationsNo() => observationsNo_;

        public List<Date> observationDates() => observationDates_;

        public List<double> observationTimes() => observationTimes_;

        public Schedule observationsSchedule() => observationsSchedule_;

        public double priceWithoutOptionality(Handle<YieldTermStructure> discountCurve) =>
            accrualPeriod() * (gearing_ * indexFixing() + spread_) *
            nominal() * discountCurve.link.discount(date());

        private double startTime_;
        private double endTime_;

        private Schedule observationsSchedule_;
        private List<Date> observationDates_;
        private List<double> observationTimes_;
        private int observationsNo_;

        private double lowerTrigger_;
        private double upperTrigger_;

    }

    [JetBrains.Annotations.PublicAPI] public class RangeAccrualPricer : FloatingRateCouponPricer
    {
        // Observer interface
        public override double swapletPrice() => throw new NotImplementedException();

        public override double swapletRate() => swapletPrice() / (accrualFactor_ * discount_);

        public override double capletPrice(double effectiveCap)
        {
            Utils.QL_FAIL("RangeAccrualPricer::capletPrice not implemented");
            return 0;
        }
        public override double capletRate(double effectiveCap)
        {
            Utils.QL_FAIL("RangeAccrualPricer::capletRate not implemented");
            return 0;
        }
        public override double floorletPrice(double effectiveFloor)
        {
            Utils.QL_FAIL("RangeAccrualPricer::floorletPrice not implemented");
            return 0;
        }
        public override double floorletRate(double effectiveFloor)
        {
            Utils.QL_FAIL("RangeAccrualPricer::floorletRate not implemented");
            return 0;
        }
        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as RangeAccrualFloatersCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "range-accrual coupon required");
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();

            var paymentDate = coupon_.date();

            var index = coupon_.index() as IborIndex;
            Utils.QL_REQUIRE(index != null, () => "invalid index");
            var rateCurve = index.forwardingTermStructure();
            discount_ = rateCurve.link.discount(paymentDate);
            accrualFactor_ = coupon_.accrualPeriod();
            spreadLegValue_ = spread_ * accrualFactor_ * discount_;

            startTime_ = coupon_.startTime();
            endTime_ = coupon_.endTime();
            observationTimes_ = coupon_.observationTimes();
            lowerTrigger_ = coupon_.lowerTrigger();
            upperTrigger_ = coupon_.upperTrigger();
            observationsNo_ = coupon_.observationsNo();

            var observationDates = coupon_.observationsSchedule().dates();
            Utils.QL_REQUIRE(observationDates.Count == observationsNo_ + 2, () => "incompatible size of initialValues vector");
            initialValues_ = new InitializedList<double>(observationDates.Count, 0.0);

            var calendar = index.fixingCalendar();
            for (var i = 0; i < observationDates.Count; i++)
            {
                initialValues_[i] = index.fixing(
                                       calendar.advance(observationDates[i], -coupon_.fixingDays, TimeUnit.Days));
            }

        }

        protected RangeAccrualFloatersCoupon coupon_;
        protected double startTime_;                                   // S
        protected double endTime_;                                     // T
        protected double accrualFactor_;                               // T-S
        protected List<double> observationTimeLags_;                   // d
        protected List<double> observationTimes_;                      // U
        protected List<double> initialValues_;
        protected int observationsNo_;
        protected double lowerTrigger_;
        protected double upperTrigger_;
        protected double discount_;
        protected double gearing_;
        protected double spread_;
        protected double spreadLegValue_;
    }

    [JetBrains.Annotations.PublicAPI] public class RangeAccrualPricerByBgm : RangeAccrualPricer
    {
        public RangeAccrualPricerByBgm(double correlation,
                                       SmileSection smilesOnExpiry,
                                       SmileSection smilesOnPayment,
                                       bool withSmile,
                                       bool byCallSpread)
        {
            correlation_ = correlation;
            withSmile_ = withSmile;
            byCallSpread_ = byCallSpread;
            smilesOnExpiry_ = smilesOnExpiry;
            smilesOnPayment_ = smilesOnPayment;
            eps_ = 1.0e-8;
        }
        // Observer interface
        public override double swapletPrice()
        {
            var result = 0.0;
            var deflator = discount_ * initialValues_[0];
            for (var i = 0; i < observationsNo_; i++)
            {
                var digitalFloater = digitalRangePrice(lowerTrigger_, upperTrigger_, initialValues_[i + 1],
                                                          observationTimes_[i], deflator);
                result += digitalFloater;
            }
            return gearing_ * (result * accrualFactor_ / observationsNo_) + spreadLegValue_;
        }

        protected double drift(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing =
               p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
               * (p * lambdaT * lambdaT + q * lambdaS * lambdaT * correlation) +
               q * lambdaS * lambdaS + p * lambdaS * lambdaT * correlation;
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5) * lambdaT * lambdaT;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }
        protected double derDriftDerLambdaS(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing = p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
                                       * (q * lambdaT * correlation) + 2 * q * lambdaS + p * lambdaT * correlation;
            var driftAfterFixing = 0.0;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }
        protected double derDriftDerLambdaT(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing = p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
                                       * (2 * p * lambdaT + q * lambdaS * correlation) + +p * lambdaS * correlation;
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5)
                                   * 2 * lambdaT;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }

        protected double lambda(double U, double lambdaS, double lambdaT)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;

            return startTime_ > 0 ? q * lambdaS + p * lambdaT : lambdaT;
        }
        protected double derLambdaDerLambdaS(double U) => startTime_ > 0 ? (endTime_ - U) / accrualFactor_ : 0.0;

        protected double derLambdaDerLambdaT(double U) => startTime_ > 0 ? (U - startTime_) / accrualFactor_ : 0.0;

        protected List<double> driftsOverPeriod(double U, double lambdaS, double lambdaT, double correlation)
        {
            var result = new List<double>();

            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing =
               p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) * (p * lambdaT * lambdaT + q * lambdaS * lambdaT * correlation) +
               q * lambdaS * lambdaS + p * lambdaS * lambdaT * correlation
               - 0.5 * lambda(U, lambdaS, lambdaT) * lambda(U, lambdaS, lambdaT);
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5) * lambdaT * lambdaT;

            result.Add(driftBeforeFixing);
            result.Add(driftAfterFixing);

            return result;
        }
        protected List<double> lambdasOverPeriod(double U, double lambdaS, double lambdaT)
        {
            var result = new List<double>();

            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;

            var lambdaBeforeFixing = q * lambdaS + p * lambdaT;
            var lambdaAfterFixing = lambdaT;

            result.Add(lambdaBeforeFixing);
            result.Add(lambdaAfterFixing);

            return result;
        }

        protected double digitalRangePrice(double lowerTrigger, double upperTrigger, double initialValue, double expiry,
                                           double deflator)
        {
            var lowerPrice = digitalPrice(lowerTrigger, initialValue, expiry, deflator);
            var upperPrice = digitalPrice(upperTrigger, initialValue, expiry, deflator);
            var result = lowerPrice - upperPrice;
            Utils.QL_REQUIRE(result > 0.0, () =>
                             "RangeAccrualPricerByBgm::digitalRangePrice:\n digitalPrice(" + upperTrigger +
                             "): " + upperPrice + " >  digitalPrice(" + lowerTrigger + "): " + lowerPrice);
            return result;
        }

        protected double digitalPrice(double strike, double initialValue, double expiry, double deflator)
        {
            var result = deflator;
            if (strike > eps_ / 2)
            {
                result = withSmile_
                         ? digitalPriceWithSmile(strike, initialValue, expiry, deflator)
                         : digitalPriceWithoutSmile(strike, initialValue, expiry, deflator);
            }
            return result;
        }

        protected double digitalPriceWithoutSmile(double strike, double initialValue, double expiry, double deflator)
        {
            var lambdaS = smilesOnExpiry_.volatility(strike);
            var lambdaT = smilesOnPayment_.volatility(strike);

            var lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
            var variance = startTime_ * lambdaU[0] * lambdaU[0] + (expiry - startTime_) * lambdaU[1] * lambdaU[1];

            var lambdaSATM = smilesOnExpiry_.volatility(initialValue);
            var lambdaTATM = smilesOnPayment_.volatility(initialValue);
            //drift of Lognormal process (of Libor) "a_U()" nel paper
            var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
            var adjustment = startTime_ * muU[0] + (expiry - startTime_) * muU[1];


            var d2 = (System.Math.Log(initialValue / strike) + adjustment - 0.5 * variance) / System.Math.Sqrt(variance);

            var phi = new CumulativeNormalDistribution();
            var result = deflator * phi.value(d2);

            Utils.QL_REQUIRE(result > 0.0, () =>
                             "RangeAccrualPricerByBgm::digitalPriceWithoutSmile: result< 0. Result:" + result);
            Utils.QL_REQUIRE(result / deflator <= 1.0, () =>
                             "RangeAccrualPricerByBgm::digitalPriceWithoutSmile: result/deflator > 1. Ratio: "
                             + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }

        protected double digitalPriceWithSmile(double strike, double initialValue, double expiry, double deflator)
        {
            double result;
            if (byCallSpread_)
            {
                // Previous strike
                var previousStrike = strike - eps_ / 2;
                var lambdaS = smilesOnExpiry_.volatility(previousStrike);
                var lambdaT = smilesOnPayment_.volatility(previousStrike);

                //drift of Lognormal process (of Libor) "a_U()" nel paper
                var lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
                var previousVariance = System.Math.Max(startTime_, 0.0) * lambdaU[0] * lambdaU[0] +
                                       System.Math.Min(expiry - startTime_, expiry) * lambdaU[1] * lambdaU[1];

                var lambdaSATM = smilesOnExpiry_.volatility(initialValue);
                var lambdaTATM = smilesOnPayment_.volatility(initialValue);
                var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
                var previousAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                         System.Math.Min(expiry - startTime_, expiry) * muU[1]);
                var previousForward = initialValue * previousAdjustment;

                // Next strike
                var nextStrike = strike + eps_ / 2;
                lambdaS = smilesOnExpiry_.volatility(nextStrike);
                lambdaT = smilesOnPayment_.volatility(nextStrike);

                lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
                var nextVariance = System.Math.Max(startTime_, 0.0) * lambdaU[0] * lambdaU[0] +
                                   System.Math.Min(expiry - startTime_, expiry) * lambdaU[1] * lambdaU[1];
                //drift of Lognormal process (of Libor) "a_U()" nel paper
                muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
                var nextAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                     System.Math.Min(expiry - startTime_, expiry) * muU[1]);
                var nextForward = initialValue * nextAdjustment;

                result = callSpreadPrice(previousForward, nextForward, previousStrike, nextStrike,
                                         deflator, previousVariance, nextVariance);
            }
            else
            {
                result = digitalPriceWithoutSmile(strike, initialValue, expiry, deflator) +
                         smileCorrection(strike, initialValue, expiry, deflator);
            }

            Utils.QL_REQUIRE(result > -System.Math.Pow(eps_, .5), () =>
                             "RangeAccrualPricerByBgm::digitalPriceWithSmile: result< 0 Result:" + result);
            Utils.QL_REQUIRE(result / deflator <= 1.0 + System.Math.Pow(eps_, .2), () =>
                             "RangeAccrualPricerByBgm::digitalPriceWithSmile: result/deflator > 1. Ratio: "
                             + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }

        protected double callSpreadPrice(double previousForward,
                                         double nextForward,
                                         double previousStrike,
                                         double nextStrike,
                                         double deflator,
                                         double previousVariance,
                                         double nextVariance)
        {
            var nextCall = Utils.blackFormula(QLNet.Option.Type.Call, nextStrike, nextForward,
                                                 System.Math.Sqrt(nextVariance), deflator);
            var previousCall = Utils.blackFormula(QLNet.Option.Type.Call, previousStrike, previousForward,
                                                     System.Math.Sqrt(previousVariance), deflator);

            Utils.QL_REQUIRE(nextCall < previousCall, () =>
                             "RangeAccrualPricerByBgm::callSpreadPrice: nextCall > previousCall" +
                             "\n nextCall: strike :" + nextStrike + "; variance: " + nextVariance +
                             " adjusted initial value " + nextForward +
                             "\n previousCall: strike :" + previousStrike + "; variance: " + previousVariance +
                             " adjusted initial value " + previousForward);

            return (previousCall - nextCall) / (nextStrike - previousStrike);
        }

        protected double smileCorrection(double strike,
                                         double forward,
                                         double expiry,
                                         double deflator)
        {
            var previousStrike = strike - eps_ / 2;
            var nextStrike = strike + eps_ / 2;

            var derSmileS = (smilesOnExpiry_.volatility(nextStrike) -
                             smilesOnExpiry_.volatility(previousStrike)) / eps_;
            var derSmileT = (smilesOnPayment_.volatility(nextStrike) -
                             smilesOnPayment_.volatility(previousStrike)) / eps_;

            var lambdaS = smilesOnExpiry_.volatility(strike);
            var lambdaT = smilesOnPayment_.volatility(strike);

            var derLambdaDerK = derLambdaDerLambdaS(expiry) * derSmileS +
                                derLambdaDerLambdaT(expiry) * derSmileT;


            var lambdaSATM = smilesOnExpiry_.volatility(forward);
            var lambdaTATM = smilesOnPayment_.volatility(forward);
            var lambdasOverPeriodU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
            //drift of Lognormal process (of Libor) "a_U()" nel paper
            var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);

            var variance = System.Math.Max(startTime_, 0.0) * lambdasOverPeriodU[0] * lambdasOverPeriodU[0] +
                           System.Math.Min(expiry - startTime_, expiry) * lambdasOverPeriodU[1] * lambdasOverPeriodU[1];

            var forwardAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                    System.Math.Min(expiry - startTime_, expiry) * muU[1]);
            var forwardAdjusted = forward * forwardAdjustment;

            var d1 = (System.Math.Log(forwardAdjusted / strike) + 0.5 * variance) / System.Math.Sqrt(variance);

            var sqrtOfTimeToExpiry = (System.Math.Max(startTime_, 0.0) * lambdasOverPeriodU[0] +
                                      System.Math.Min(expiry - startTime_, expiry) * lambdasOverPeriodU[1]) * (1.0 / System.Math.Sqrt(variance));

            var phi = new CumulativeNormalDistribution();
            var psi = new NormalDistribution();
            var result = -forwardAdjusted * psi.value(d1) * sqrtOfTimeToExpiry * derLambdaDerK;

            result *= deflator;

            Utils.QL_REQUIRE(System.Math.Abs(result / deflator) <= 1.0 + System.Math.Pow(eps_, .2), () =>
                             "RangeAccrualPricerByBgm::smileCorrection: abs(result/deflator) > 1. Ratio: "
                             + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }


        private double correlation_;   // correlation between L(S) and L(T)
        private bool withSmile_;
        private bool byCallSpread_;

        private SmileSection smilesOnExpiry_;
        private SmileSection smilesOnPayment_;
        private double eps_;
    }

    //! helper class building a sequence of range-accrual floating-rate coupons
    [JetBrains.Annotations.PublicAPI] public class RangeAccrualLeg
    {
        public RangeAccrualLeg(Schedule schedule, IborIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
            observationConvention_ = BusinessDayConvention.ModifiedFollowing;
        }
        public RangeAccrualLeg withNotionals(double notional)
        {
            notionals_ = new InitializedList<double>(1, notional);
            return this;
        }
        public RangeAccrualLeg withNotionals(List<double> notionals)
        {
            notionals_ = notionals;
            return this;
        }
        public RangeAccrualLeg withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }
        public RangeAccrualLeg withPaymentAdjustment(BusinessDayConvention convention)
        {
            paymentAdjustment_ = convention;
            return this;
        }
        public RangeAccrualLeg withFixingDays(int fixingDays)
        {
            fixingDays_ = new InitializedList<int>(1, fixingDays);
            return this;
        }
        public RangeAccrualLeg withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }
        public RangeAccrualLeg withGearings(double gearing)
        {
            gearings_ = new InitializedList<double>(1, gearing);
            return this;
        }
        public RangeAccrualLeg withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }
        public RangeAccrualLeg withSpreads(double spread)
        {
            spreads_ = new InitializedList<double>(1, spread);
            return this;
        }
        public RangeAccrualLeg withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }
        public RangeAccrualLeg withLowerTriggers(double trigger)
        {
            lowerTriggers_ = new InitializedList<double>(1, trigger);
            return this;
        }
        public RangeAccrualLeg withLowerTriggers(List<double> triggers)
        {
            lowerTriggers_ = triggers;
            return this;
        }
        public RangeAccrualLeg withUpperTriggers(double trigger)
        {
            upperTriggers_ = new InitializedList<double>(1, trigger);
            return this;
        }
        public RangeAccrualLeg withUpperTriggers(List<double> triggers)
        {
            upperTriggers_ = triggers;
            return this;
        }
        public RangeAccrualLeg withObservationTenor(Period tenor)
        {
            observationTenor_ = tenor;
            return this;
        }
        public RangeAccrualLeg withObservationConvention(BusinessDayConvention convention)
        {
            observationConvention_ = convention;
            return this;
        }
        public List<CashFlow> Leg()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var n = schedule_.Count - 1;
            Utils.QL_REQUIRE(notionals_.Count <= n, () =>
                             "too many nominals (" + notionals_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(fixingDays_.Count <= n, () =>
                             "too many fixingDays (" + fixingDays_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(gearings_.Count <= n, () =>
                             "too many gearings (" + gearings_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(spreads_.Count <= n, () =>
                             "too many spreads (" + spreads_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(lowerTriggers_.Count <= n, () =>
                             "too many lowerTriggers (" + lowerTriggers_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(upperTriggers_.Count <= n, () =>
                             "too many upperTriggers (" + upperTriggers_.Count + "), only " + n + " required");

            var leg = new List<CashFlow>();


            // the following is not always correct
            var calendar = schedule_.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;
            var observationsSchedules = new List<Schedule>();

            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule_.date(i);
                refEnd = end = schedule_.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refStart = calendar.adjust(end - schedule_.tenor(), bdc);
                }
                if (i == n - 1 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refEnd = calendar.adjust(start + schedule_.tenor(), bdc);
                }
                if (gearings_.Get(i, 1.0).IsEqual(0.0))
                {
                    // fixed coupon
                    leg.Add(new FixedRateCoupon(paymentDate,
                                                notionals_.Get(i),
                                                spreads_.Get(i, 0.0),
                                                paymentDayCounter_,
                                                start, end, refStart, refEnd));
                }
                else
                {
                    // floating coupon
                    observationsSchedules.Add(new Schedule(start, end,
                                                           observationTenor_, calendar,
                                                           observationConvention_,
                                                           observationConvention_,
                                                           DateGeneration.Rule.Forward, false));

                    leg.Add(new RangeAccrualFloatersCoupon(paymentDate,
                                                           notionals_.Get(i),
                                                           index_,
                                                           start, end,
                                                           fixingDays_.Get(i, 2),
                                                           paymentDayCounter_,
                                                           gearings_.Get(i, 1.0),
                                                           spreads_.Get(i, 0.0),
                                                           refStart, refEnd,
                                                           observationsSchedules.Last(),
                                                           lowerTriggers_.Get(i),
                                                           upperTriggers_.Get(i)));
                }
            }
            return leg;

        }


        private Schedule schedule_;
        private IborIndex index_;
        private List<double> notionals_;
        private DayCounter paymentDayCounter_;
        private BusinessDayConvention paymentAdjustment_;
        private List<int> fixingDays_;
        private List<double> gearings_;
        private List<double> spreads_;
        private List<double> lowerTriggers_, upperTriggers_;
        private Period observationTenor_;
        private BusinessDayConvention observationConvention_;
    }
}
