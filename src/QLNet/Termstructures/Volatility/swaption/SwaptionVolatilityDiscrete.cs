﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.swaption
{
    public abstract class SwaptionVolatilityDiscrete : SwaptionVolatilityStructure
    {
        protected Date evaluationDate_;
        protected int nOptionTenors_;
        protected int nSwapTenors_;
        protected List<Date> optionDates_;
        protected List<double> optionDatesAsReal_;
        protected Interpolation optionInterpolator_;
        protected List<Period> optionTenors_;
        protected List<double> optionTimes_;
        protected List<double> swapLengths_;
        protected List<Period> swapTenors_;

        protected SwaptionVolatilityDiscrete(List<Period> optionTenors,
            List<Period> swapTenors,
            int settlementDays,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc)
            : base(settlementDays, cal, bdc, dc)
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            optionDatesAsReal_ = new InitializedList<double>(nOptionTenors_);
            nSwapTenors_ = swapTenors.Count;
            swapTenors_ = swapTenors;
            swapLengths_ = new InitializedList<double>(nSwapTenors_);

            checkOptionTenors();
            initializeOptionDatesAndTimes();

            checkSwapTenors();
            initializeSwapLengths();

            optionInterpolator_ = new LinearInterpolation(optionTimes_, optionTimes_.Count, optionDatesAsReal_);
            optionInterpolator_.update();
            optionInterpolator_.enableExtrapolation();
            evaluationDate_ = Settings.evaluationDate();
            Settings.registerWith(update);
        }

        protected SwaptionVolatilityDiscrete(List<Period> optionTenors,
            List<Period> swapTenors,
            Date referenceDate,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc)
            : base(referenceDate, cal, bdc, dc)
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            optionDatesAsReal_ = new InitializedList<double>(nOptionTenors_);
            nSwapTenors_ = swapTenors.Count;
            swapTenors_ = swapTenors;
            swapLengths_ = new InitializedList<double>(nSwapTenors_);

            checkOptionTenors();
            initializeOptionDatesAndTimes();

            checkSwapTenors();
            initializeSwapLengths();

            optionInterpolator_ = new LinearInterpolation(optionTimes_, optionTimes_.Count, optionDatesAsReal_);

            optionInterpolator_.update();
            optionInterpolator_.enableExtrapolation();
        }

        protected SwaptionVolatilityDiscrete(List<Date> optionDates,
            List<Period> swapTenors,
            Date referenceDate,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc)
            : base(referenceDate, cal, bdc, dc)
        {
            nOptionTenors_ = optionDates.Count;
            optionTenors_ = new InitializedList<Period>(nOptionTenors_);
            optionDates_ = optionDates;
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            optionDatesAsReal_ = new InitializedList<double>(nOptionTenors_);
            nSwapTenors_ = swapTenors.Count;
            swapTenors_ = swapTenors;
            swapLengths_ = new InitializedList<double>(nSwapTenors_);

            checkOptionDates();
            initializeOptionTimes();

            checkSwapTenors();
            initializeSwapLengths();

            optionInterpolator_ = new LinearInterpolation(optionTimes_, optionTimes_.Count, optionDatesAsReal_);
            optionInterpolator_.update();
            optionInterpolator_.enableExtrapolation();
        }

        //! additional inspectors
        public Date optionDateFromTime(double optionTime) => new Date((int)optionInterpolator_.value(optionTime));

        public List<Date> optionDates() => optionDates_;

        public List<Period> optionTenors() => optionTenors_;

        public List<double> optionTimes() => optionTimes_;

        public List<double> swapLengths() => swapLengths_;

        public List<Period> swapTenors() => swapTenors_;

        public override void update()
        {
            if (moving_)
            {
                var d = Settings.evaluationDate();
                if (evaluationDate_ != d)
                {
                    evaluationDate_ = d;
                    initializeOptionDatesAndTimes();
                    initializeSwapLengths();
                }
            }

            base.update();
        }

        /* In case a pricing engine is not used, this method must be overridden to perform the actual
           calculations and set any needed results.
         * In case a pricing engine is used, the default implementation can be used. */

        protected override void performCalculations()
        {
            // check if date recalculation could be avoided here
            if (moving_)
            {
                initializeOptionDatesAndTimes();
                initializeSwapLengths();
                optionInterpolator_.update();
            }
        }

        private void checkOptionDates()
        {
            QLNet.Utils.QL_REQUIRE(optionDates_[0] > referenceDate(), () =>
                "first option date (" + optionDates_[0] + ") must be greater than reference date (" + referenceDate() + ")");
            for (var i = 1; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(optionDates_[i] > optionDates_[i - 1], () =>
                    "non increasing option dates: " + i + " is " + optionDates_[i - 1] + ", " + i + 1 + " is " + optionDates_[i]);
            }
        }

        private void checkOptionTenors()
        {
            QLNet.Utils.QL_REQUIRE(optionTenors_[0] > new Period(0, TimeUnit.Days), () =>
                "first option tenor is negative (" + optionTenors_[0] + ")");
            for (var i = 1; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(optionTenors_[i] > optionTenors_[i - 1], () =>
                    "non increasing option tenor: " + i + " is " + optionTenors_[i - 1] + ", " + i + 1 + " is " + optionTenors_[i]);
            }
        }

        private void checkSwapTenors()
        {
            QLNet.Utils.QL_REQUIRE(swapTenors_[0] > new Period(0, TimeUnit.Days), () =>
                "first swap tenor is negative (" + swapTenors_[0] + ")");
            for (var i = 1; i < nSwapTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(swapTenors_[i] > swapTenors_[i - 1], () =>
                    "non increasing swap tenor: " + i + " is " + swapTenors_[i - 1] + ", " + i + 1 + " is " + swapTenors_[i]);
            }
        }

        private void initializeOptionDatesAndTimes()
        {
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                optionDates_[i] = optionDateFromTenor(optionTenors_[i]);
                optionDatesAsReal_[i] = optionDates_[i].serialNumber();
            }

            initializeOptionTimes();
        }

        private void initializeOptionTimes()
        {
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                optionTimes_[i] = timeFromReference(optionDates_[i]);
            }
        }

        private void initializeSwapLengths()
        {
            for (var i = 0; i < nSwapTenors_; ++i)
            {
                swapLengths_[i] = swapLength(swapTenors_[i]);
            }
        }
    }
}
