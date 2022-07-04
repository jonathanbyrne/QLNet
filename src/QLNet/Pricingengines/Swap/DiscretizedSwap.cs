/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
using QLNet.Time;
using System.Collections.Generic;
using QLNet.Math;

namespace QLNet.Pricingengines.Swap
{
    [JetBrains.Annotations.PublicAPI] public class DiscretizedSwap : DiscretizedAsset
    {

        private VanillaSwap.Arguments arguments_;
        private List<double> fixedResetTimes_;
        private List<double> fixedPayTimes_;
        private List<double> floatingResetTimes_;
        private List<double> floatingPayTimes_;

        public DiscretizedSwap(VanillaSwap.Arguments args,
                               Date referenceDate,
                               DayCounter dayCounter)
        {
            arguments_ = args;
            fixedResetTimes_ = new InitializedList<double>(args.fixedResetDates.Count);
            for (var i = 0; i < fixedResetTimes_.Count; ++i)
                fixedResetTimes_[i] =
                   dayCounter.yearFraction(referenceDate,
                                           args.fixedResetDates[i]);

            fixedPayTimes_ = new InitializedList<double>(args.fixedPayDates.Count);
            for (var i = 0; i < fixedPayTimes_.Count; ++i)
                fixedPayTimes_[i] =
                   dayCounter.yearFraction(referenceDate,
                                           args.fixedPayDates[i]);

            floatingResetTimes_ = new InitializedList<double>(args.floatingResetDates.Count);
            for (var i = 0; i < floatingResetTimes_.Count; ++i)
                floatingResetTimes_[i] =
                   dayCounter.yearFraction(referenceDate,
                                           args.floatingResetDates[i]);

            floatingPayTimes_ = new InitializedList<double>(args.floatingPayDates.Count);
            for (var i = 0; i < floatingPayTimes_.Count; ++i)
                floatingPayTimes_[i] =
                   dayCounter.yearFraction(referenceDate,
                                           args.floatingPayDates[i]);
        }

        public override void reset(int size)
        {
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        public override List<double> mandatoryTimes()
        {
            var times = new List<double>();
            for (var i = 0; i < fixedResetTimes_.Count; i++)
            {
                var t = fixedResetTimes_[i];
                if (t >= 0.0)
                    times.Add(t);
            }
            for (var i = 0; i < fixedPayTimes_.Count; i++)
            {
                var t = fixedPayTimes_[i];
                if (t >= 0.0)
                    times.Add(t);
            }
            for (var i = 0; i < floatingResetTimes_.Count; i++)
            {
                var t = floatingResetTimes_[i];
                if (t >= 0.0)
                    times.Add(t);
            }
            for (var i = 0; i < floatingPayTimes_.Count; i++)
            {
                var t = floatingPayTimes_[i];
                if (t >= 0.0)
                    times.Add(t);
            }
            return times;
        }

        protected override void preAdjustValuesImpl()
        {
            // floating payments
            for (var i = 0; i < floatingResetTimes_.Count; i++)
            {
                var t = floatingResetTimes_[i];
                if (t >= 0.0 && isOnTime(t))
                {
                    var bond = new DiscretizedDiscountBond();
                    bond.initialize(method(), floatingPayTimes_[i]);
                    bond.rollback(time_);

                    var nominal = arguments_.nominal;
                    var T = arguments_.floatingAccrualTimes[i];
                    var spread = arguments_.floatingSpreads[i];
                    var accruedSpread = nominal * T * spread;
                    for (var j = 0; j < values_.size(); j++)
                    {
                        var coupon = nominal * (1.0 - bond.values()[j])
                                     + accruedSpread * bond.values()[j];
                        if (arguments_.type == VanillaSwap.Type.Payer)
                            values_[j] += coupon;
                        else
                            values_[j] -= coupon;
                    }
                }
            }
            // fixed payments
            for (var i = 0; i < fixedResetTimes_.Count; i++)
            {
                var t = fixedResetTimes_[i];
                if (t >= 0.0 && isOnTime(t))
                {
                    var bond = new DiscretizedDiscountBond();
                    bond.initialize(method(), fixedPayTimes_[i]);
                    bond.rollback(time_);

                    var fixedCoupon = arguments_.fixedCoupons[i];
                    for (var j = 0; j < values_.size(); j++)
                    {
                        var coupon = fixedCoupon * bond.values()[j];
                        if (arguments_.type == VanillaSwap.Type.Payer)
                            values_[j] -= coupon;
                        else
                            values_[j] += coupon;
                    }
                }
            }
        }

        protected override void postAdjustValuesImpl()
        {
            // fixed coupons whose reset time is in the past won't be managed
            // in preAdjustValues()
            for (var i = 0; i < fixedPayTimes_.Count; i++)
            {
                var t = fixedPayTimes_[i];
                var reset = fixedResetTimes_[i];
                if (t >= 0.0 && isOnTime(t) && reset < 0.0)
                {
                    var fixedCoupon = arguments_.fixedCoupons[i];
                    if (arguments_.type == VanillaSwap.Type.Payer)
                        values_ -= fixedCoupon;
                    else
                        values_ += fixedCoupon;
                }
            }
            // the same applies to floating payments whose rate is already fixed
            for (var i = 0; i < floatingPayTimes_.Count; i++)
            {
                var t = floatingPayTimes_[i];
                var reset = floatingResetTimes_[i];
                if (t >= 0.0 && isOnTime(t) && reset < 0.0)
                {
                    var currentFloatingCoupon = arguments_.floatingCoupons[i];
                    if (arguments_.type == VanillaSwap.Type.Payer)
                        values_ += currentFloatingCoupon;
                    else
                        values_ -= currentFloatingCoupon;
                }
            }
        }
    }
}
