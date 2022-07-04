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
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Pricingengines.vanilla;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Pricingengines.barrier
{
    [JetBrains.Annotations.PublicAPI] public class DiscretizedBarrierOption : DiscretizedAsset
    {
        public DiscretizedBarrierOption(BarrierOption.Arguments args, StochasticProcess process, TimeGrid grid = null)
        {
            arguments_ = args;
            vanilla_ = new DiscretizedVanillaOption(arguments_, process, grid);

            Utils.QL_REQUIRE(args.exercise.dates().Count > 0, () => "specify at least one stopping date");

            stoppingTimes_ = new InitializedList<double>(args.exercise.dates().Count);
            for (var i = 0; i < stoppingTimes_.Count; ++i)
            {
                stoppingTimes_[i] = process.time(args.exercise.date(i));
                if (grid != null && !grid.empty())
                {
                    // adjust to the given grid
                    stoppingTimes_[i] = grid.closestTime(stoppingTimes_[i]);
                }
            }
        }

        public override void reset(int size)
        {
            vanilla_.initialize(method(), time());
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        public Vector vanilla() => vanilla_.values();

        public BarrierOption.Arguments arguments() => arguments_;

        public override List<double> mandatoryTimes() => stoppingTimes_;

        public void checkBarrier(Vector optvalues, Vector grid)
        {
            var now = time();
            var endTime = isOnTime(stoppingTimes_.Last());
            var stoppingTime = false;
            switch (arguments_.exercise.ExerciseType())
            {
                case Exercise.Type.American:
                    if (now <= stoppingTimes_[1] &&
                        now >= stoppingTimes_[0])
                        stoppingTime = true;
                    break;
                case Exercise.Type.European:
                    if (isOnTime(stoppingTimes_[0]))
                        stoppingTime = true;
                    break;
                case Exercise.Type.Bermudan:
                    for (var i = 0; i < stoppingTimes_.Count; i++)
                    {
                        if (isOnTime(stoppingTimes_[i]))
                        {
                            stoppingTime = true;
                            break;
                        }
                    }
                    break;
                default:
                    Utils.QL_FAIL("invalid option ExerciseType");
                    break;
            }
            for (var j = 0; j < optvalues.size(); j++)
            {
                switch (arguments_.barrierType)
                {
                    case Barrier.Type.DownIn:
                        if (grid[j] <= arguments_.barrier)
                        {
                            // knocked in
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla_.values()[j];
                        }
                        else if (endTime)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault();
                        break;
                    case Barrier.Type.DownOut:
                        if (grid[j] <= arguments_.barrier)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out
                        else if (stoppingTime)
                        {
                            optvalues[j] = System.Math.Max(optvalues[j], arguments_.payoff.value(grid[j]));
                        }
                        break;
                    case Barrier.Type.UpIn:
                        if (grid[j] >= arguments_.barrier)
                        {
                            // knocked in
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla_.values()[j];
                        }
                        else if (endTime)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault();
                        break;
                    case Barrier.Type.UpOut:
                        if (grid[j] >= arguments_.barrier)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out
                        else if (stoppingTime)
                            optvalues[j] = System.Math.Max(optvalues[j], arguments_.payoff.value(grid[j]));
                        break;
                    default:
                        Utils.QL_FAIL("invalid barrier ExerciseType");
                        break;
                }
            }
        }

        protected override void postAdjustValuesImpl()
        {
            if (arguments_.barrierType == Barrier.Type.DownIn ||
                arguments_.barrierType == Barrier.Type.UpIn)
            {
                vanilla_.rollback(time());
            }
            var grid = method().grid(time());
            checkBarrier(values_, grid);
        }

        private BarrierOption.Arguments arguments_;
        private List<double> stoppingTimes_;
        private DiscretizedVanillaOption vanilla_;

    }
}
