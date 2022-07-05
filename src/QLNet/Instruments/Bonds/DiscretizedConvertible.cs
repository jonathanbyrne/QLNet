//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.processes;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class DiscretizedConvertible : DiscretizedAsset
    {
        public Vector conversionProbability_;
        public Vector dividendValues_;
        public Vector spreadAdjustedRate_;
        private ConvertibleBond.option.Arguments arguments_;
        private List<double> callabilityTimes_;
        private List<double> couponTimes_;
        private List<double> dividendTimes_;
        private GeneralizedBlackScholesProcess process_;
        private List<double> stoppingTimes_;

        public DiscretizedConvertible(ConvertibleBond.option.Arguments args,
            GeneralizedBlackScholesProcess process, TimeGrid grid)
        {
            arguments_ = args;
            process_ = process;

            dividendValues_ = new Vector(arguments_.dividends.Count, 0.0);

            var settlementDate = process.riskFreeRate().link.referenceDate();
            for (var i = 0; i < arguments_.dividends.Count; i++)
            {
                if (arguments_.dividends[i].date() >= settlementDate)
                {
                    dividendValues_[i] = arguments_.dividends[i].amount() *
                                         process.riskFreeRate().link.discount(arguments_.dividends[i].date());
                }
            }

            var dayCounter = process.riskFreeRate().currentLink().dayCounter();
            var bondSettlement = arguments_.settlementDate;

            stoppingTimes_ = new InitializedList<double>(arguments_.exercise.dates().Count, 0.0);
            for (var i = 0; i < stoppingTimes_.Count; i++)
            {
                stoppingTimes_[i] = dayCounter.yearFraction(bondSettlement, arguments_.exercise.date(i));
            }

            callabilityTimes_ = new InitializedList<double>(arguments_.callabilityDates.Count, 0.0);
            for (var i = 0; i < callabilityTimes_.Count; i++)
            {
                callabilityTimes_[i] = dayCounter.yearFraction(bondSettlement, arguments_.callabilityDates[i]);
            }

            couponTimes_ = new InitializedList<double>(arguments_.couponDates.Count, 0.0);
            for (var i = 0; i < couponTimes_.Count; i++)
            {
                couponTimes_[i] = dayCounter.yearFraction(bondSettlement, arguments_.couponDates[i]);
            }

            dividendTimes_ = new InitializedList<double>(arguments_.dividendDates.Count, 0.0);
            for (var i = 0; i < dividendTimes_.Count; i++)
            {
                dividendTimes_[i] = dayCounter.yearFraction(bondSettlement, arguments_.dividendDates[i]);
            }

            if (!grid.empty())
            {
                // adjust times to grid
                for (var i = 0; i < stoppingTimes_.Count; i++)
                {
                    stoppingTimes_[i] = grid.closestTime(stoppingTimes_[i]);
                }

                for (var i = 0; i < couponTimes_.Count; i++)
                {
                    couponTimes_[i] = grid.closestTime(couponTimes_[i]);
                }

                for (var i = 0; i < dividendTimes_.Count; i++)
                {
                    dividendTimes_[i] = grid.closestTime(dividendTimes_[i]);
                }

                for (var i = 0; i < callabilityTimes_.Count; i++)
                {
                    callabilityTimes_[i] = grid.closestTime(callabilityTimes_[i]);
                }
            }
        }

        public void addCoupon(int i)
        {
            values_ += arguments_.couponAmounts[i];
        }

        public Vector adjustedGrid()
        {
            var t = time();
            var grid = method().grid(t);
            // add back all dividend amounts in the future
            for (var i = 0; i < arguments_.dividends.Count; i++)
            {
                var dividendTime = dividendTimes_[i];
                if (dividendTime >= t || Utils.close(dividendTime, t))
                {
                    var d = arguments_.dividends[i];
                    var dividendDiscount = process_.riskFreeRate().currentLink().discount(dividendTime) /
                                           process_.riskFreeRate().currentLink().discount(t);
                    for (var j = 0; j < grid.size(); j++)
                    {
                        grid[j] += d.amount(grid[j]) * dividendDiscount;
                    }
                }
            }

            return grid;
        }

        public void applyCallability(int i, bool convertible)
        {
            var grid = adjustedGrid();
            switch (arguments_.callabilityTypes[i])
            {
                case Callability.Type.Call:
                    if (arguments_.callabilityTriggers[i] != null)
                    {
                        var conversionValue = arguments_.redemption / arguments_.conversionRatio;
                        var trigger = conversionValue * arguments_.callabilityTriggers[i];

                        for (var j = 0; j < values_.size(); j++)
                            // the callability is conditioned by the trigger ...
                        {
                            if (grid[j] >= trigger)
                            {
                                // .. and might trigger conversion
                                values_[j] =
                                    System.Math.Min(
                                        System.Math.Max(arguments_.callabilityPrices[i],
                                            Convert.ToDouble(arguments_.conversionRatio) * grid[j]), values_[j]);
                            }
                        }
                    }
                    else if (convertible)
                    {
                        for (var j = 0; j < values_.size(); j++)
                        {
                            // exercising the callability might trigger conversion
                            values_[j] =
                                System.Math.Min(
                                    System.Math.Max(arguments_.callabilityPrices[i],
                                        Convert.ToDouble(arguments_.conversionRatio) * grid[j]), values_[j]);
                        }
                    }
                    else
                    {
                        for (var j = 0; j < values_.size(); j++)
                        {
                            values_[j] = System.Math.Min(arguments_.callabilityPrices[i], values_[j]);
                        }
                    }

                    break;
                case Callability.Type.Put:
                    for (var j = 0; j < values_.size(); j++)
                    {
                        values_[j] = System.Math.Max(values_[j], arguments_.callabilityPrices[i]);
                    }

                    break;
                default:
                    Utils.QL_FAIL("unknown callability ExerciseType ");
                    break;
            }
        }

        public void applyConvertibility()
        {
            var grid = adjustedGrid();
            for (var j = 0; j < values_.size(); j++)
            {
                var payoff = Convert.ToDouble(arguments_.conversionRatio) * grid[j];
                if (values_[j] <= payoff)
                {
                    values_[j] = payoff;
                    conversionProbability_[j] = 1.0;
                }
            }
        }

        public ConvertibleBond.option.Arguments arguments() => arguments_;

        public Vector conversionProbability() => conversionProbability_;

        public Vector dividendValues() => dividendValues_;

        public override List<double> mandatoryTimes()
        {
            var result = new List<double>();
            result.AddRange(stoppingTimes_);
            result.AddRange(callabilityTimes_);
            result.AddRange(couponTimes_);
            return result;
        }

        public GeneralizedBlackScholesProcess process() => process_;

        public override void reset(int size)
        {
            // Set to bond redemption values
            values_ = new Vector(size, Convert.ToDouble(arguments_.redemption));

            conversionProbability_ = new Vector(size, 0.0);
            spreadAdjustedRate_ = new Vector(size, 0.0);

            var rfdc = process_.riskFreeRate().link.dayCounter();

            // this takes care of the convertibility and conversion probabilities
            adjustValues();

            var creditSpread = arguments_.creditSpread;
            var exercise = arguments_.exercise.lastDate();
            var riskFreeRate = process_.riskFreeRate().link
                .zeroRate(exercise, rfdc, Compounding.Continuous, Frequency.NoFrequency);

            // Claculate blended discount rate to be used on roll back .
            for (var j = 0; j < values_.Count; j++)
            {
                spreadAdjustedRate_[j] = conversionProbability_[j] * riskFreeRate.value() +
                                         (1 - conversionProbability_[j]) *
                                         (riskFreeRate.value() + creditSpread.link.value());
            }
        }

        public Vector spreadAdjustedRate() => spreadAdjustedRate_;

        protected override void postAdjustValuesImpl()
        {
            var convertible = false;
            switch (arguments_.exercise.ExerciseType())
            {
                case Exercise.Type.American:
                    if (time() <= stoppingTimes_[1] && time() >= stoppingTimes_[0])
                    {
                        convertible = true;
                    }

                    break;
                case Exercise.Type.European:
                    if (isOnTime(stoppingTimes_[0]))
                    {
                        convertible = true;
                    }

                    break;
                case Exercise.Type.Bermudan:
                    for (var i = 0; i < stoppingTimes_.Count; i++)
                    {
                        if (isOnTime(stoppingTimes_[i]))
                        {
                            convertible = true;
                        }
                    }

                    break;
                default:
                    Utils.QL_FAIL("invalid option ExerciseType ");
                    break;
            }

            for (var i = 0; i < callabilityTimes_.Count; i++)
            {
                if (isOnTime(callabilityTimes_[i]))
                {
                    applyCallability(i, convertible);
                }
            }

            for (var i = 0; i < couponTimes_.Count; i++)
            {
                if (isOnTime(couponTimes_[i]))
                {
                    addCoupon(i);
                }
            }

            if (convertible)
            {
                applyConvertibility();
            }
        }
    }
}
