﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class DiscretizedVanillaOption : DiscretizedAsset
    {
        private QLNet.Option.Arguments arguments_;
        private List<double> stoppingTimes_;

        public DiscretizedVanillaOption(QLNet.Option.Arguments args, StochasticProcess process, TimeGrid grid)
        {
            arguments_ = args;

            stoppingTimes_ = new InitializedList<double>(args.exercise.dates().Count);
            for (var i = 0; i < stoppingTimes_.Count; ++i)
            {
                stoppingTimes_[i] = process.time(args.exercise.date(i));
                if (!grid.empty())
                {
                    // adjust to the given grid
                    stoppingTimes_[i] = grid.closestTime(stoppingTimes_[i]);
                }
            }
        }

        public override List<double> mandatoryTimes() => stoppingTimes_;

        public override void reset(int size)
        {
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        protected override void postAdjustValuesImpl()
        {
            var now = time();
            switch (arguments_.exercise.ExerciseType())
            {
                case Exercise.Type.American:
                    if (now <= stoppingTimes_[1] && now >= stoppingTimes_[0])
                    {
                        applySpecificCondition();
                    }

                    break;
                case Exercise.Type.European:
                    if (isOnTime(stoppingTimes_[0]))
                    {
                        applySpecificCondition();
                    }

                    break;
                case Exercise.Type.Bermudan:
                    for (var i = 0; i < stoppingTimes_.Count; i++)
                    {
                        if (isOnTime(stoppingTimes_[i]))
                        {
                            applySpecificCondition();
                        }
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid option ExerciseType");
                    break;
            }
        }

        private void applySpecificCondition()
        {
            var grid = method().grid(time());
            for (var j = 0; j < values_.size(); j++)
            {
                values_[j] = System.Math.Max(values_[j], arguments_.payoff.value(grid[j]));
            }
        }
    }
}
