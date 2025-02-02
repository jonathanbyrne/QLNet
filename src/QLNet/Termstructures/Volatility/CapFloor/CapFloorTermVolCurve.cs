﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Quotes;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Termstructures.Volatility.CapFloor
{
    //! Cap/floor at-the-money term-volatility vector
    /*! This class provides the at-the-money volatility for a given cap/floor
        interpolating a volatility vector whose elements are the market
        volatilities of a set of caps/floors with given length.
    */
    [PublicAPI]
    public class CapFloorTermVolCurve : CapFloorTermVolatilityStructure
    {
        private Date evaluationDate_;

        // make it not mutable if possible
        private Interpolation interpolation_;
        private int nOptionTenors_;
        private List<Date> optionDates_;
        private List<Period> optionTenors_;
        private List<double> optionTimes_;
        private List<Handle<Quote>> volHandles_;
        private List<double> vols_;

        //! floating reference date, floating market data
        public CapFloorTermVolCurve(int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<Handle<Quote>> vols,
            DayCounter dc = null) // Actual365Fixed()
            : base(settlementDays, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            volHandles_ = vols;
            vols_ = new InitializedList<double>(vols.Count); // do not initialize with nOptionTenors_

            checkInputs();
            initializeOptionDatesAndTimes();
            registerWithMarketData();
            interpolate();
        }

        //! fixed reference date, floating market data
        public CapFloorTermVolCurve(Date settlementDate,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<Handle<Quote>> vols,
            DayCounter dc = null) // Actual365Fixed()
            : base(settlementDate, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            volHandles_ = vols;
            vols_ = new InitializedList<double>(vols.Count); // do not initialize with nOptionTenors_

            checkInputs();
            initializeOptionDatesAndTimes();
            registerWithMarketData();
            interpolate();
        }

        //! fixed reference date, fixed market data
        public CapFloorTermVolCurve(Date settlementDate,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> vols,
            DayCounter dc = null) // Actual365Fixed()
            : base(settlementDate, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            volHandles_ = new InitializedList<Handle<Quote>>(vols.Count);
            vols_ = vols; // do not initialize with nOptionTenors_

            checkInputs();
            initializeOptionDatesAndTimes();
            // fill dummy handles to allow generic handle-based computations later
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                volHandles_[i] = new Handle<Quote>(new SimpleQuote(vols_[i]));
            }

            interpolate();
        }

        //! floating reference date, fixed market data
        public CapFloorTermVolCurve(int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> vols,
            DayCounter dc = null) // Actual365Fixed()
            : base(settlementDays, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            volHandles_ = new InitializedList<Handle<Quote>>(vols.Count);
            vols_ = vols; // do not initialize with nOptionTenors_

            checkInputs();
            initializeOptionDatesAndTimes();
            // fill dummy handles to allow generic handle-based computations later
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                volHandles_[i] = new Handle<Quote>(new SimpleQuote(vols_[i]));
            }

            interpolate();
        }

        // TermStructure interface
        public override Date maxDate()
        {
            calculate();
            return optionDateFromTenor(optionTenors_.Last());
        }

        public override double maxStrike() => double.MaxValue;

        // VolatilityTermStructure interface
        public override double minStrike() => double.MinValue;

        public List<Date> optionDates()
        {
            // what if quotes are not available?
            calculate();
            return optionDates_;
        }

        // some inspectors
        public List<Period> optionTenors() => optionTenors_;

        public List<double> optionTimes()
        {
            // what if quotes are not available?
            calculate();
            return optionTimes_;
        }

        // LazyObject interface
        public override void update()
        {
            // recalculate dates if necessary...
            if (moving_)
            {
                var d = Settings.evaluationDate();
                if (evaluationDate_ != d)
                {
                    evaluationDate_ = d;
                    initializeOptionDatesAndTimes();
                }
            }

            base.update();
        }

        protected override void performCalculations()
        {
            // check if date recalculation must be called here

            for (var i = 0; i < vols_.Count; ++i)
            {
                vols_[i] = volHandles_[i].link.value();
            }

            interpolation_.update();
        }

        protected override double volatilityImpl(double t, double r)
        {
            calculate();
            return interpolation_.value(t, true);
        }

        private void checkInputs()
        {
            QLNet.Utils.QL_REQUIRE(!optionTenors_.empty(), () => "empty option tenor vector");
            QLNet.Utils.QL_REQUIRE(nOptionTenors_ == vols_.Count, () =>
                "mismatch between number of option tenors (" +
                nOptionTenors_ + ") and number of volatilities (" +
                vols_.Count + ")");
            QLNet.Utils.QL_REQUIRE(optionTenors_[0] > new Period(0, TimeUnit.Days), () =>
                "negative first option tenor: " + optionTenors_[0]);
            for (var i = 1; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(optionTenors_[i] > optionTenors_[i - 1], () =>
                    "non increasing option tenor: " + i +
                    " is " + optionTenors_[i - 1] + ", " +
                    (i + 1) + " is " + optionTenors_[i]);
            }
        }

        private void initializeOptionDatesAndTimes()
        {
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                optionDates_[i] = optionDateFromTenor(optionTenors_[i]);
                optionTimes_[i] = timeFromReference(optionDates_[i]);
            }
        }

        private void interpolate()
        {
            interpolation_ = new CubicInterpolation(optionTimes_, optionTimes_.Count, vols_,
                CubicInterpolation.DerivativeApprox.Spline, false,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
        }

        private void registerWithMarketData()
        {
            for (var i = 0; i < volHandles_.Count; ++i)
            {
                volHandles_[i].registerWith(update);
            }
        }
    }
}
