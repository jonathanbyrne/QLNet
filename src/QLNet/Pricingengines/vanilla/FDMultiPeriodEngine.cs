﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class FDMultiPeriodEngine : FDConditionEngineTemplate
    {
        protected List<Event> events_ = new List<Event>();
        protected FiniteDifferenceModel<CrankNicolson<TridiagonalOperator>> model_;
        protected List<double> stoppingTimes_ = new List<double>();
        protected int timeStepPerPeriod_;

        // required for generics
        public FDMultiPeriodEngine()
        {
        }

        //protected FDMultiPeriodEngine(GeneralizedBlackScholesProcess process,
        //     int gridPoints = 100, int timeSteps = 100, bool timeDependent = false)
        protected FDMultiPeriodEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
            timeStepPerPeriod_ = timeSteps;
        }

        public override void calculate(IPricingEngineResults r)
        {
            var results = r as OneAssetOption.Results;
            QLNet.Utils.QL_REQUIRE(results != null, () => "incorrect results ExerciseType");

            double beginDate, endDate;
            var dateNumber = stoppingTimes_.Count;
            var lastDateIsResTime = false;
            var firstIndex = -1;
            var lastIndex = dateNumber - 1;
            var firstDateIsZero = false;
            var firstNonZeroDate = getResidualTime();

            var dateTolerance = 1e-6;
            int j;

            if (dateNumber > 0)
            {
                QLNet.Utils.QL_REQUIRE(getDividendTime(0) >= 0, () => "first date (" + getDividendTime(0) + ") cannot be negative");
                if (getDividendTime(0) < getResidualTime() * dateTolerance)
                {
                    firstDateIsZero = true;
                    firstIndex = 0;
                    if (dateNumber >= 2)
                    {
                        firstNonZeroDate = getDividendTime(1);
                    }
                }

                if (System.Math.Abs(getDividendTime(lastIndex) - getResidualTime()) < dateTolerance)
                {
                    lastDateIsResTime = true;
                    lastIndex = dateNumber - 2;
                }

                if (!firstDateIsZero)
                {
                    firstNonZeroDate = getDividendTime(0);
                }

                if (dateNumber >= 2)
                {
                    for (j = 1; j < dateNumber; j++)
                    {
                        QLNet.Utils.QL_REQUIRE(getDividendTime(j - 1) < getDividendTime(j), () =>
                            "dates must be in increasing order: " + getDividendTime(j - 1) +
                            " is not strictly smaller than " + getDividendTime(j));
                    }
                }
            }

            var dt = getResidualTime() / (timeStepPerPeriod_ * (dateNumber + 1));

            // Ensure that dt is always smaller than the first non-zero date
            if (firstNonZeroDate <= dt)
            {
                dt = firstNonZeroDate / 2.0;
            }

            setGridLimits();
            initializeInitialCondition();
            initializeOperator();
            initializeBoundaryConditions();
            initializeModel();
            initializeStepCondition();

            prices_ = (SampledCurve)intrinsicValues_.Clone();
            if (lastDateIsResTime)
            {
                executeIntermediateStep(dateNumber - 1);
            }

            j = lastIndex;
            object temp;
            do
            {
                if (j == dateNumber - 1)
                {
                    beginDate = getResidualTime();
                }
                else
                {
                    beginDate = getDividendTime(j + 1);
                }

                if (j >= 0)
                {
                    endDate = getDividendTime(j);
                }
                else
                {
                    endDate = dt;
                }

                temp = prices_.values();
                model_.rollback(ref temp, beginDate, endDate, timeStepPerPeriod_, stepCondition_);
                prices_.setValues((Vector)temp);

                if (j >= 0)
                {
                    executeIntermediateStep(j);
                }
            } while (--j >= firstIndex);

            temp = prices_.values();
            model_.rollback(ref temp, dt, 0, 1, stepCondition_);
            prices_.setValues((Vector)temp);

            if (firstDateIsZero)
            {
                executeIntermediateStep(0);
            }

            results.value = prices_.valueAtCenter();
            results.delta = prices_.firstDerivativeAtCenter();
            results.gamma = prices_.secondDerivativeAtCenter();
            results.additionalResults["priceCurve"] = prices_;
        }

        public override void setupArguments(IPricingEngineArguments a)
        {
            base.setupArguments(a);
            var args = a as QLNet.Option.Arguments;
            QLNet.Utils.QL_REQUIRE(args != null, () => "incorrect argument ExerciseType");
            events_.Clear();

            var n = args.exercise.dates().Count;
            stoppingTimes_ = new InitializedList<double>(n);
            for (var i = 0; i < n; ++i)
            {
                stoppingTimes_[i] = process_.time(args.exercise.date(i));
            }
        }

        protected virtual void executeIntermediateStep(int step)
        {
            throw new NotSupportedException();
        }

        protected double getDividendTime(int i) => stoppingTimes_[i];

        protected virtual void initializeModel()
        {
            model_ = new FiniteDifferenceModel<CrankNicolson<TridiagonalOperator>>(finiteDifferenceOperator_, BCs_);
        }

        protected virtual void setupArguments(IPricingEngineArguments args, List<Event> schedule)
        {
            base.setupArguments(args);
            events_ = schedule;
            stoppingTimes_.Clear();
            var n = schedule.Count;
            stoppingTimes_ = new List<double>(n);
            for (var i = 0; i < n; ++i)
            {
                stoppingTimes_.Add(process_.time(events_[i].date()));
            }
        }
    }
}
