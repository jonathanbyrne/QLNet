﻿/*
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.PricingEngines.CapFloor
{
    [PublicAPI]
    public class DiscretizedCapFloor : DiscretizedAsset
    {
        private Instruments.CapFloor.Arguments arguments_;
        private List<double> endTimes_;
        private List<double> startTimes_;

        public DiscretizedCapFloor(Instruments.CapFloor.Arguments args,
            Date referenceDate,
            DayCounter dayCounter)
        {
            arguments_ = args;

            startTimes_ = new InitializedList<double>(args.startDates.Count);
            for (var i = 0; i < startTimes_.Count; ++i)
            {
                startTimes_[i] = dayCounter.yearFraction(referenceDate,
                    args.startDates[i]);
            }

            endTimes_ = new InitializedList<double>(args.endDates.Count);
            for (var i = 0; i < endTimes_.Count; ++i)
            {
                endTimes_[i] = dayCounter.yearFraction(referenceDate,
                    args.endDates[i]);
            }
        }

        public override List<double> mandatoryTimes()
        {
            var times = startTimes_;

            for (var j = 0; j < endTimes_.Count; j++)
            {
                times.Insert(0, endTimes_[j]);
            }

            return times;
        }

        public override void reset(int size)
        {
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        protected override void postAdjustValuesImpl()
        {
            for (var i = 0; i < endTimes_.Count; i++)
            {
                if (isOnTime(endTimes_[i]))
                {
                    if (startTimes_[i] < 0.0)
                    {
                        var nominal = arguments_.nominals[i];
                        var accrual = arguments_.accrualTimes[i];
                        var fixing = (double)arguments_.forwards[i];
                        var gearing = arguments_.gearings[i];
                        var type = arguments_.type;

                        if (type == CapFloorType.Cap || type == CapFloorType.Collar)
                        {
                            var cap = (double)arguments_.capRates[i];
                            var capletRate = System.Math.Max(fixing - cap, 0.0);
                            values_ += capletRate * accrual * nominal * gearing;
                        }

                        if (type == CapFloorType.Floor || type == CapFloorType.Collar)
                        {
                            var floor = (double)arguments_.floorRates[i];
                            var floorletRate = System.Math.Max(floor - fixing, 0.0);
                            if (type == CapFloorType.Floor)
                            {
                                values_ += floorletRate * accrual * nominal * gearing;
                            }
                            else
                            {
                                values_ -= floorletRate * accrual * nominal * gearing;
                            }
                        }
                    }
                }
            }
        }

        protected override void preAdjustValuesImpl()
        {
            for (var i = 0; i < startTimes_.Count; i++)
            {
                if (isOnTime(startTimes_[i]))
                {
                    var end = endTimes_[i];
                    var tenor = arguments_.accrualTimes[i];
                    var bond = new DiscretizedDiscountBond();
                    bond.initialize(method(), end);
                    bond.rollback(time_);

                    var type = arguments_.type;
                    var gearing = arguments_.gearings[i];
                    var nominal = arguments_.nominals[i];

                    if (type == CapFloorType.Cap ||
                        type == CapFloorType.Collar)
                    {
                        var accrual = (double)(1.0 + arguments_.capRates[i] * tenor);
                        var strike = 1.0 / accrual;
                        for (var j = 0; j < values_.size(); j++)
                        {
                            values_[j] += nominal * accrual * gearing *
                                          System.Math.Max(strike - bond.values()[j], 0.0);
                        }
                    }

                    if (type == CapFloorType.Floor ||
                        type == CapFloorType.Collar)
                    {
                        var accrual = (double)(1.0 + arguments_.floorRates[i] * tenor);
                        var strike = 1.0 / accrual;
                        var mult = type == CapFloorType.Floor ? 1.0 : -1.0;
                        for (var j = 0; j < values_.size(); j++)
                        {
                            values_[j] += nominal * accrual * mult * gearing *
                                          System.Math.Max(bond.values()[j] - strike, 0.0);
                        }
                    }
                }
            }
        }
    }
}
