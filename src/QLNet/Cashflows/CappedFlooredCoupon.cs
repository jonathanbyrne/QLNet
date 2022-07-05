/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2009 Siarhei Novik (snovik@gmail.com)
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

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    /// <summary>
    ///     Capped and/or floored floating-rate coupon
    ///     <remarks>
    ///         The payoff P of a capped floating-rate coupon is: P=N×T×min(aL+b,C).
    ///         The payoff of a floored floating-rate coupon is:  P=N×T×max(aL+b,F).
    ///         The payoff of a collared floating-rate coupon is: P=N×T×min(max(aL+b,F),C).
    ///         where N is the notional, T is the accrual time, L is the floating rate, a is its gearing, b is the spread, and
    ///         C and F the strikes.
    ///         They can be decomposed in the following manner. Decomposition of a capped floating rate coupon:
    ///         R=min(aL+b,C)=(aL+b)+min(C?b??|a|L,0)
    ///         where ?=sgn(a). Then: R=(aL+b)+|a|min(C?b|a|??L,0)
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class CappedFlooredCoupon : FloatingRateCoupon
    {
        protected double? _cap;
        protected double? _floor;
        protected bool _isCapped;
        protected bool _isFloored;
        // data
        protected FloatingRateCoupon _underlying;

        // need by CashFlowVectors
        public CappedFlooredCoupon()
        {
        }

        public CappedFlooredCoupon(FloatingRateCoupon underlying, double? cap = null, double? floor = null)
            : base(underlying.date(), underlying.nominal(), underlying.accrualStartDate(), underlying.accrualEndDate(), underlying.fixingDays, underlying.index(), underlying.gearing(), underlying.spread(), underlying.referencePeriodStart, underlying.referencePeriodEnd, underlying.dayCounter(), underlying.isInArrears())
        {
            _underlying = underlying;
            _isCapped = false;
            _isFloored = false;

            if (gearing_ > 0)
            {
                if (cap != null)
                {
                    _isCapped = true;
                    _cap = cap;
                }

                if (floor != null)
                {
                    _floor = floor;
                    _isFloored = true;
                }
            }
            else
            {
                if (cap != null)
                {
                    _floor = cap;
                    _isFloored = true;
                }

                if (floor != null)
                {
                    _isCapped = true;
                    _cap = floor;
                }
            }

            if (_isCapped && _isFloored)
            {
                Utils.QL_REQUIRE(cap >= floor, () =>
                    "cap level (" + cap + ") less than floor level (" + floor + ")");
            }

            underlying.registerWith(update);
        }

        // cap
        public double Cap()
        {
            if (gearing_ > 0 && _isCapped)
            {
                return _cap.GetValueOrDefault();
            }

            if (gearing_ < 0 && _isFloored)
            {
                return _floor.GetValueOrDefault();
            }

            return 0.0;
        }

        public override double convexityAdjustment() => _underlying.convexityAdjustment();

        //! effective cap of fixing
        public double? EffectiveCap() => _isCapped ? (_cap.Value - spread()) / gearing() : (double?)null;

        //! effective floor of fixing
        public double? EffectiveFloor() => _isFloored ? (_floor.Value - spread()) / gearing() : (double?)null;

        // Factory - for Leg generators
        public virtual CashFlow Factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, InterestRateIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears) => new CappedFlooredCoupon(new IborCoupon(paymentDate, nominal, startDate, endDate, fixingDays, (IborIndex)index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears), cap, floor);

        //! floor
        public double Floor()
        {
            if (gearing_ > 0 && _isFloored)
            {
                return _floor.GetValueOrDefault();
            }

            if (gearing_ < 0 && _isCapped)
            {
                return _cap.GetValueOrDefault();
            }

            return 0.0;
        }

        public bool IsCapped() => _isCapped;

        public bool IsFloored() => _isFloored;

        // Coupon interface
        public override double rate()
        {
            Utils.QL_REQUIRE(_underlying.pricer() != null, () => "pricer not set");

            var swapletRate = _underlying.rate();
            var floorletRate = 0.0;
            if (_isFloored)
            {
                floorletRate = _underlying.pricer().floorletRate(EffectiveFloor().Value);
            }

            var capletRate = 0.0;
            if (_isCapped)
            {
                capletRate = _underlying.pricer().capletRate(EffectiveCap().Value);
            }

            return swapletRate + floorletRate - capletRate;
        }

        public override void setPricer(FloatingRateCouponPricer pricer)
        {
            base.setPricer(pricer);
            _underlying.setPricer(pricer);
        }
    }
}
