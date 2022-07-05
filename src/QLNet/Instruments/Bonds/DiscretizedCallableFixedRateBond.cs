/*
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class DiscretizedCallableFixedRateBond : DiscretizedAsset
    {
        private CallableBond.Arguments arguments_;
        private List<double> callabilityTimes_ = new List<double>();
        private List<double> couponTimes_ = new List<double>();
        private double redemptionTime_;

        public DiscretizedCallableFixedRateBond(CallableBond.Arguments args,
            Date referenceDate,
            DayCounter dayCounter)
        {
            arguments_ = args;
            redemptionTime_ = dayCounter.yearFraction(referenceDate, args.redemptionDate);

            for (var i = 0; i < args.couponDates.Count; ++i)
            {
                couponTimes_.Add(dayCounter.yearFraction(referenceDate, args.couponDates[i]));
            }

            for (var i = 0; i < args.callabilityDates.Count; ++i)
            {
                callabilityTimes_.Add(dayCounter.yearFraction(referenceDate, args.callabilityDates[i]));
            }

            // similar to the tree swaption engine, we collapse similar coupon
            // and exercise dates to avoid mispricing. Delete if unnecessary.

            for (var i = 0; i < callabilityTimes_.Count; i++)
            {
                var exerciseTime = callabilityTimes_[i];
                for (var j = 0; j < couponTimes_.Count; j++)
                {
                    if (withinNextWeek(exerciseTime, couponTimes_[j]))
                    {
                        couponTimes_[j] = exerciseTime;
                    }
                }
            }
        }

        public override List<double> mandatoryTimes()
        {
            var times = new List<double>();
            double t;
            int i;

            t = redemptionTime_;
            if (t >= 0.0)
            {
                times.Add(t);
            }

            for (i = 0; i < couponTimes_.Count; i++)
            {
                t = couponTimes_[i];
                if (t >= 0.0)
                {
                    times.Add(t);
                }
            }

            for (i = 0; i < callabilityTimes_.Count; i++)
            {
                t = callabilityTimes_[i];
                if (t >= 0.0)
                {
                    times.Add(t);
                }
            }

            return times;
        }

        public override void reset(int size)
        {
            values_ = new Vector(size, arguments_.redemption);
            adjustValues();
        }

        protected override void postAdjustValuesImpl()
        {
            for (var i = 0; i < callabilityTimes_.Count; i++)
            {
                var t = callabilityTimes_[i];
                if (t >= 0.0 && isOnTime(t))
                {
                    applyCallability(i);
                }
            }

            for (var i = 0; i < couponTimes_.Count; i++)
            {
                var t = couponTimes_[i];
                if (t >= 0.0 && isOnTime(t))
                {
                    addCoupon(i);
                }
            }
        }

        protected override void preAdjustValuesImpl()
        {
            // Nothing to do here
        }

        private void addCoupon(int i)
        {
            values_ += arguments_.couponAmounts[i];
        }

        private void applyCallability(int i)
        {
            int j;
            switch (arguments_.putCallSchedule[i].type())
            {
                case Callability.Type.Call:
                    for (j = 0; j < values_.size(); j++)
                    {
                        values_[j] = System.Math.Min(arguments_.callabilityPrices[i], values_[j]);
                    }

                    break;

                case Callability.Type.Put:
                    for (j = 0; j < values_.size(); j++)
                    {
                        values_[j] = System.Math.Max(values_[j], arguments_.callabilityPrices[i]);
                    }

                    break;

                default:
                    QLNet.Utils.QL_FAIL("unknown callability ExerciseType");
                    break;
            }
        }

        private bool withinNextWeek(double t1, double t2)
        {
            var dt = 1.0 / 52;
            return t1 <= t2 && t2 <= t1 + dt;
        }
    }
}
