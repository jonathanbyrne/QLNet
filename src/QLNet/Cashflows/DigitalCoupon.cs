/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Indexes;

namespace QLNet.Cashflows
{
    //! Digital-payoff coupon
    //    ! Implementation of a floating-rate coupon with digital call/put option.
    //        Payoffs:
    //        - Coupon with cash-or-nothing Digital Call
    //          rate + csi * payoffRate * Heaviside(rate-strike)
    //        - Coupon with cash-or-nothing Digital Put
    //          rate + csi * payoffRate * Heaviside(strike-rate)
    //        where csi=+1 or csi=-1.
    //        - Coupon with asset-or-nothing Digital Call
    //          rate + csi * rate * Heaviside(rate-strike)
    //        - Coupon with asset-or-nothing Digital Put
    //          rate + csi * rate * Heaviside(strike-rate)
    //        where csi=+1 or csi=-1.
    //        The evaluation of the coupon is made using the call/put spread
    //        replication method.
    //
    //    ! \ingroup instruments
    //
    //        \test
    //        - the correctness of the returned value in case of Asset-or-nothing
    //          embedded option is tested by pricing the digital option with
    //          Cox-Rubinstein formula.
    //        - the correctness of the returned value in case of deep-in-the-money
    //          Asset-or-nothing embedded option is tested vs the expected values of
    //          coupon and option.
    //        - the correctness of the returned value in case of deep-out-of-the-money
    //          Asset-or-nothing embedded option is tested vs the expected values of
    //          coupon and option.
    //        - the correctness of the returned value in case of Cash-or-nothing
    //          embedded option is tested by pricing the digital option with
    //          Reiner-Rubinstein formula.
    //        - the correctness of the returned value in case of deep-in-the-money
    //          Cash-or-nothing embedded option is tested vs the expected values of
    //          coupon and option.
    //        - the correctness of the returned value in case of deep-out-of-the-money
    //          Cash-or-nothing embedded option is tested vs the expected values of
    //          coupon and option.
    //        - the correctness of the returned value is tested checking the correctness
    //          of the call-put parity relation.
    //        - the correctness of the returned value is tested by the relationship
    //          between prices in case of different replication types.
    //
    [PublicAPI]
    public class DigitalCoupon : FloatingRateCoupon
    {
        //! multiplicative factor of call payoff
        protected double callCsi_;
        //! digital call option payoff rate, if any
        protected double callDigitalPayoff_;
        //! the left and right gaps applied in payoff replication for call
        protected double callLeftEps_;
        protected double callRightEps_;
        //! strike rate for the the call option
        protected double callStrike_;
        protected bool hasCallStrike_;
        //!
        protected bool hasPutStrike_;
        //! inclusion flag og the call payoff if the call option ends at-the-money
        protected bool isCallATMIncluded_;
        //! digital call option ExerciseType: if true, cash-or-nothing, if false asset-or-nothing
        protected bool isCallCashOrNothing_;
        //! inclusion flag og the put payoff if the put option ends at-the-money
        protected bool isPutATMIncluded_;
        //! digital put option ExerciseType: if true, cash-or-nothing, if false asset-or-nothing
        protected bool isPutCashOrNothing_;
        //! multiplicative factor of put payoff
        protected double putCsi_;
        //! digital put option payoff rate, if any
        protected double putDigitalPayoff_;
        //! the left and right gaps applied in payoff replication for puf
        protected double putLeftEps_;
        protected double putRightEps_;
        //! strike rate for the the put option
        protected double putStrike_;
        //! Type of replication
        protected Replication.Type replicationType_;

        // Data members
        protected FloatingRateCoupon underlying_;

        // need by CashFlowVectors
        public DigitalCoupon()
        {
        }

        //! Constructors
        //! general constructor
        public DigitalCoupon(FloatingRateCoupon underlying,
            double? callStrike = null,
            Position.Type callPosition = Position.Type.Long,
            bool isCallATMIncluded = false,
            double? callDigitalPayoff = null,
            double? putStrike = null,
            Position.Type putPosition = Position.Type.Long,
            bool isPutATMIncluded = false,
            double? putDigitalPayoff = null,
            DigitalReplication replication = null)
            : base(underlying.date(), underlying.nominal(), underlying.accrualStartDate(), underlying.accrualEndDate(), underlying.fixingDays, underlying.index(), underlying.gearing(), underlying.spread(), underlying.referencePeriodStart, underlying.referencePeriodEnd, underlying.dayCounter(), underlying.isInArrears())
        {
            if (replication == null)
            {
                replication = new DigitalReplication();
            }

            underlying_ = underlying;
            callCsi_ = 0.0;
            putCsi_ = 0.0;
            isCallATMIncluded_ = isCallATMIncluded;
            isPutATMIncluded_ = isPutATMIncluded;
            isCallCashOrNothing_ = false;
            isPutCashOrNothing_ = false;
            callLeftEps_ = replication.gap() / 2.0;
            callRightEps_ = replication.gap() / 2.0;
            putLeftEps_ = replication.gap() / 2.0;
            putRightEps_ = replication.gap() / 2.0;
            hasPutStrike_ = false;
            hasCallStrike_ = false;
            replicationType_ = replication.replicationType();

            QLNet.Utils.QL_REQUIRE(replication.gap() > 0.0, () => "Non positive epsilon not allowed");

            if (putStrike == null)
            {
                QLNet.Utils.QL_REQUIRE(putDigitalPayoff == null, () => "Put Cash rate non allowed if put strike is null");
            }

            if (callStrike == null)
            {
                QLNet.Utils.QL_REQUIRE(callDigitalPayoff == null, () => "Call Cash rate non allowed if call strike is null");
            }

            if (callStrike != null)
            {
                QLNet.Utils.QL_REQUIRE(callStrike >= 0.0, () => "negative call strike not allowed");

                hasCallStrike_ = true;
                callStrike_ = callStrike.GetValueOrDefault();
                QLNet.Utils.QL_REQUIRE(callStrike_ >= replication.gap() / 2.0, () => "call strike < eps/2");

                switch (callPosition)
                {
                    case Position.Type.Long:
                        callCsi_ = 1.0;
                        break;
                    case Position.Type.Short:
                        callCsi_ = -1.0;
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                        break;
                }

                if (callDigitalPayoff != null)
                {
                    callDigitalPayoff_ = callDigitalPayoff.GetValueOrDefault();
                    isCallCashOrNothing_ = true;
                }
            }

            if (putStrike != null)
            {
                QLNet.Utils.QL_REQUIRE(putStrike >= 0.0, () => "negative put strike not allowed");
                hasPutStrike_ = true;
                putStrike_ = putStrike.GetValueOrDefault();
                switch (putPosition)
                {
                    case Position.Type.Long:
                        putCsi_ = 1.0;
                        break;
                    case Position.Type.Short:
                        putCsi_ = -1.0;
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                        break;
                }

                if (putDigitalPayoff != null)
                {
                    putDigitalPayoff_ = putDigitalPayoff.GetValueOrDefault();
                    isPutCashOrNothing_ = true;
                }
            }

            switch (replicationType_)
            {
                case Replication.Type.Central:
                    // do nothing
                    break;
                case Replication.Type.Sub:
                    if (hasCallStrike_)
                    {
                        switch (callPosition)
                        {
                            case Position.Type.Long:
                                callLeftEps_ = 0.0;
                                callRightEps_ = replication.gap();
                                break;
                            case Position.Type.Short:
                                callLeftEps_ = replication.gap();
                                callRightEps_ = 0.0;
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                                break;
                        }
                    }

                    if (hasPutStrike_)
                    {
                        switch (putPosition)
                        {
                            case Position.Type.Long:
                                putLeftEps_ = replication.gap();
                                putRightEps_ = 0.0;
                                break;
                            case Position.Type.Short:
                                putLeftEps_ = 0.0;
                                putRightEps_ = replication.gap();
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                                break;
                        }
                    }

                    break;
                case Replication.Type.Super:
                    if (hasCallStrike_)
                    {
                        switch (callPosition)
                        {
                            case Position.Type.Long:
                                callLeftEps_ = replication.gap();
                                callRightEps_ = 0.0;
                                break;
                            case Position.Type.Short:
                                callLeftEps_ = 0.0;
                                callRightEps_ = replication.gap();
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                                break;
                        }
                    }

                    if (hasPutStrike_)
                    {
                        switch (putPosition)
                        {
                            case Position.Type.Long:
                                putLeftEps_ = 0.0;
                                putRightEps_ = replication.gap();
                                break;
                            case Position.Type.Short:
                                putLeftEps_ = replication.gap();
                                putRightEps_ = 0.0;
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                                break;
                        }
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("unsupported position ExerciseType");
                    break;
            }

            underlying.registerWith(update);
        }

        public double? callDigitalPayoff()
        {
            if (isCallCashOrNothing_)
            {
                return callDigitalPayoff_;
            }

            return null;
        }

        //        ! Returns the call option rate
        //           (multiplied by: nominal*accrualperiod*discount is the NPV of the option)
        //
        public double callOptionRate()
        {
            var callOptionRate = 0.0;
            if (hasCallStrike_)
            {
                // Step function
                callOptionRate = isCallCashOrNothing_ ? callDigitalPayoff_ : callStrike_;
                var next = new CappedFlooredCoupon(underlying_, callStrike_ + callRightEps_);
                var previous = new CappedFlooredCoupon(underlying_, callStrike_ - callLeftEps_);
                callOptionRate *= (next.rate() - previous.rate()) / (callLeftEps_ + callRightEps_);
                if (!isCallCashOrNothing_)
                {
                    // Call
                    var atStrike = new CappedFlooredCoupon(underlying_, callStrike_);
                    var call = underlying_.rate() - atStrike.rate();
                    // Sum up
                    callOptionRate += call;
                }
            }

            return callOptionRate;
        }

        // Digital inspectors
        public double? callStrike()
        {
            if (hasCall())
            {
                return callStrike_;
            }

            return null;
        }

        public override double convexityAdjustment() => underlying_.convexityAdjustment();

        // Factory - for Leg generators
        public virtual CashFlow factory(FloatingRateCoupon underlying, double? callStrike, Position.Type callPosition, bool isCallATMIncluded, double? callDigitalPayoff, double? putStrike, Position.Type putPosition, bool isPutATMIncluded, double? putDigitalPayoff, DigitalReplication replication) => new DigitalCoupon(underlying, callStrike, callPosition, isCallATMIncluded, callDigitalPayoff, putStrike, putPosition, isPutATMIncluded, putDigitalPayoff, replication);

        public bool hasCall() => hasCallStrike_;

        public bool hasCollar() => hasCallStrike_ && hasPutStrike_;

        public bool hasPut() => hasPutStrike_;

        public bool isLongCall() => callCsi_.IsEqual(1.0);

        public bool isLongPut() => putCsi_.IsEqual(1.0);

        public double? putDigitalPayoff()
        {
            if (isPutCashOrNothing_)
            {
                return putDigitalPayoff_;
            }

            return null;
        }

        //        ! Returns the put option rate
        //           (multiplied by: nominal*accrualperiod*discount is the NPV of the option)
        //
        public double putOptionRate()
        {
            var putOptionRate = 0.0;
            if (hasPutStrike_)
            {
                // Step function
                putOptionRate = isPutCashOrNothing_ ? putDigitalPayoff_ : putStrike_;
                var next = new CappedFlooredCoupon(underlying_, null, putStrike_ + putRightEps_);
                var previous = new CappedFlooredCoupon(underlying_, null, putStrike_ - putLeftEps_);
                putOptionRate *= (next.rate() - previous.rate()) / (putLeftEps_ + putRightEps_);
                if (!isPutCashOrNothing_)
                {
                    // Put
                    var atStrike = new CappedFlooredCoupon(underlying_, null, putStrike_);
                    var put = -underlying_.rate() + atStrike.rate();
                    // Sum up
                    putOptionRate -= put;
                }
            }

            return putOptionRate;
        }

        public double? putStrike()
        {
            if (hasPut())
            {
                return putStrike_;
            }

            return null;
        }

        // Coupon interface
        public override double rate()
        {
            QLNet.Utils.QL_REQUIRE(underlying_.pricer() != null, () => "pricer not set");

            var fixingDate = underlying_.fixingDate();
            var today = Settings.evaluationDate();
            var enforceTodaysHistoricFixings = Settings.enforcesTodaysHistoricFixings;
            var underlyingRate = underlying_.rate();
            if (fixingDate < today || fixingDate == today && enforceTodaysHistoricFixings)
            {
                // must have been fixed
                return underlyingRate + callCsi_ * callPayoff() + putCsi_ * putPayoff();
            }

            if (fixingDate == today)
            {
                // might have been fixed
                var pastFixing = IndexManager.instance().getHistory(underlying_.index().name())[fixingDate];
                if (pastFixing != null)
                {
                    return underlyingRate + callCsi_ * callPayoff() + putCsi_ * putPayoff();
                }

                return underlyingRate + callCsi_ * callOptionRate() + putCsi_ * putOptionRate();
            }

            return underlyingRate + callCsi_ * callOptionRate() + putCsi_ * putOptionRate();
        }

        public override void setPricer(FloatingRateCouponPricer pricer)
        {
            if (pricer_ != null)
            {
                pricer_.unregisterWith(update);
            }

            pricer_ = pricer;
            if (pricer_ != null)
            {
                pricer_.registerWith(update);
            }

            update();
            underlying_.setPricer(pricer);
        }

        public FloatingRateCoupon underlying() => underlying_;

        private double callPayoff()
        {
            // to use only if index has fixed
            var payoff = 0.0;
            if (hasCallStrike_)
            {
                var underlyingRate = underlying_.rate();
                if (underlyingRate - callStrike_ > 1.0e-16)
                {
                    payoff = isCallCashOrNothing_ ? callDigitalPayoff_ : underlyingRate;
                }
                else
                {
                    if (isCallATMIncluded_ && System.Math.Abs(callStrike_ - underlyingRate) <= 1.0e-16)
                    {
                        payoff = isCallCashOrNothing_ ? callDigitalPayoff_ : underlyingRate;
                    }
                }
            }

            return payoff;
        }

        private double putPayoff()
        {
            // to use only if index has fixed
            var payoff = 0.0;
            if (hasPutStrike_)
            {
                var underlyingRate = underlying_.rate();
                if (putStrike_ - underlyingRate > 1.0e-16)
                {
                    payoff = isPutCashOrNothing_ ? putDigitalPayoff_ : underlyingRate;
                }
                else
                {
                    // putStrike_ <= underlyingRate
                    if (isPutATMIncluded_)
                    {
                        if (System.Math.Abs(putStrike_ - underlyingRate) <= 1.0e-16)
                        {
                            payoff = isPutCashOrNothing_ ? putDigitalPayoff_ : underlyingRate;
                        }
                    }
                }
            }

            return payoff;
        }
    }
}
