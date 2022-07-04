//  Copyright (C) 2015 Thema Consulting SA
//  Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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
    [JetBrains.Annotations.PublicAPI] public class DiscretizedDoubleBarrierOption : DiscretizedAsset
    {
        public DiscretizedDoubleBarrierOption(DoubleBarrierOption.Arguments args, StochasticProcess process, TimeGrid grid = null)
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

        public DoubleBarrierOption.Arguments arguments() => arguments_;

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
                    case DoubleBarrier.Type.KnockIn:
                        if (grid[j] <= arguments_.barrier_lo)
                        {
                            // knocked in dn
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla_.values()[j];
                        }
                        else if (grid[j] >= arguments_.barrier_hi)
                        {
                            // knocked in up
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla()[j];
                        }
                        else if (endTime)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault();
                        break;
                    case DoubleBarrier.Type.KnockOut:
                        if (grid[j] <= arguments_.barrier_lo)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out lo
                        else if (grid[j] >= arguments_.barrier_hi)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out lo
                        else if (stoppingTime)
                        {
                            optvalues[j] = System.Math.Max(optvalues[j], arguments_.payoff.value(grid[j]));
                        }
                        break;
                    case DoubleBarrier.Type.KIKO:
                        // low barrier is KI, high is KO
                        if (grid[j] <= arguments_.barrier_lo)
                        {
                            // knocked in dn
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla()[j];
                        }
                        else if (grid[j] >= arguments_.barrier_hi)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out hi
                        else if (endTime)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault();
                        break;
                    case DoubleBarrier.Type.KOKI:
                        // low barrier is KO, high is KI
                        if (grid[j] <= arguments_.barrier_lo)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out lo
                        else if (grid[j] >= arguments_.barrier_hi)
                        {
                            // knocked in up
                            if (stoppingTime)
                            {
                                optvalues[j] = System.Math.Max(vanilla_.values()[j], arguments_.payoff.value(grid[j]));
                            }
                            else
                                optvalues[j] = vanilla()[j];
                        }
                        else if (endTime)
                            optvalues[j] = arguments_.rebate.GetValueOrDefault();
                        break;
                    default:
                        Utils.QL_FAIL("invalid barrier ExerciseType");
                        break;
                }
            }
        }

        protected override void postAdjustValuesImpl()
        {
            if (arguments_.barrierType != DoubleBarrier.Type.KnockOut)
            {
                vanilla_.rollback(time());
            }
            var grid = method().grid(time());
            checkBarrier(values_, grid);
        }

        private DoubleBarrierOption.Arguments arguments_;
        private List<double> stoppingTimes_;
        private DiscretizedVanillaOption vanilla_;

    }

    //! Derman-Kani-Ergener-Bardhan discretized option helper class
    /*! This class is used with the BinomialDoubleBarrierEngine to
        implement the enhanced binomial algorithm of E.Derman, I.Kani,
        D.Ergener, I.Bardhan ("Enhanced Numerical Methods for Options with
        Barriers", 1995)

        \note This algorithm is only suitable if the payoff can be approximated
        linearly, e.g. is not usable for cash-or-nothing payoffs.
    */
    [JetBrains.Annotations.PublicAPI] public class DiscretizedDermanKaniDoubleBarrierOption : DiscretizedAsset
    {
        public DiscretizedDermanKaniDoubleBarrierOption(DoubleBarrierOption.Arguments args,
                                                        StochasticProcess process, TimeGrid grid = null)
        {
            unenhanced_ = new DiscretizedDoubleBarrierOption(args, process, grid);
        }

        public override void reset(int size)
        {
            unenhanced_.initialize(method(), time());
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        public override List<double> mandatoryTimes() => unenhanced_.mandatoryTimes();

        protected override void postAdjustValuesImpl()
        {
            unenhanced_.rollback(time());

            var grid = method().grid(time());
            unenhanced_.checkBarrier(values_, grid);   // compute payoffs
            adjustBarrier(values_, grid);
        }

        private void adjustBarrier(Vector optvalues, Vector grid)
        {
            var barrier_lo = unenhanced_.arguments().barrier_lo;
            var barrier_hi = unenhanced_.arguments().barrier_hi;
            var rebate = unenhanced_.arguments().rebate;
            switch (unenhanced_.arguments().barrierType)
            {
                case DoubleBarrier.Type.KnockIn:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier_lo && grid[j + 1] > barrier_lo)
                        {
                            // grid[j+1] above barrier_lo, grid[j] under (in),
                            // interpolate optvalues[j+1]
                            var ltob = barrier_lo - grid[j];
                            var htob = grid[j + 1] - barrier_lo;
                            var htol = grid[j + 1] - grid[j];
                            var u1 = unenhanced_.values()[j + 1];
                            var t1 = unenhanced_.vanilla()[j + 1];
                            optvalues[j + 1] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * t1 + htob.GetValueOrDefault() * u1) / htol); // derman std
                        }
                        else if (grid[j] < barrier_hi && grid[j + 1] >= barrier_hi)
                        {
                            // grid[j+1] above barrier_hi (in), grid[j] under,
                            // interpolate optvalues[j]
                            var ltob = barrier_hi - grid[j];
                            var htob = grid[j + 1] - barrier_hi;
                            var htol = grid[j + 1] - grid[j];
                            var u = unenhanced_.values()[j];
                            var t = unenhanced_.vanilla()[j];
                            optvalues[j] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * u + htob.GetValueOrDefault() * t) / htol); // derman std
                        }
                    }
                    break;
                case DoubleBarrier.Type.KnockOut:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier_lo && grid[j + 1] > barrier_lo)
                        {
                            // grid[j+1] above barrier_lo, grid[j] under (out),
                            // interpolate optvalues[j+1]
                            var a = (barrier_lo - grid[j]) * rebate;
                            var b = (grid[j + 1] - barrier_lo) * unenhanced_.values()[j + 1];
                            var c = grid[j + 1] - grid[j];
                            optvalues[j + 1] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                        else if (grid[j] < barrier_hi && grid[j + 1] >= barrier_hi)
                        {
                            // grid[j+1] above barrier_hi (out), grid[j] under,
                            // interpolate optvalues[j]
                            var a = (barrier_hi - grid[j]) * unenhanced_.values()[j];
                            var b = (grid[j + 1] - barrier_hi) * rebate;
                            var c = grid[j + 1] - grid[j];
                            optvalues[j] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                    }
                    break;
                default:
                    Utils.QL_FAIL("unsupported barrier ExerciseType");
                    break;
            }
        }
        private DiscretizedDoubleBarrierOption unenhanced_;
    }
}
