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
    [PublicAPI]
    public class CapFloorTermVolSurface : CapFloorTermVolatilityStructure
    {
        private Date evaluationDate_;

        // make it not mutable if possible
        private Interpolation2D interpolation_;
        private int nOptionTenors_;
        private int nStrikes_;
        private List<Date> optionDates_;
        private List<Period> optionTenors_;
        private List<double> optionTimes_;
        private List<double> strikes_;
        private List<List<Handle<Quote>>> volHandles_;
        private Matrix vols_;

        //! floating reference date, floating market data
        public CapFloorTermVolSurface(int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> strikes,
            List<List<Handle<Quote>>> vols,
            DayCounter dc = null)
            : base(settlementDays, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            nStrikes_ = strikes.Count;
            strikes_ = strikes;
            volHandles_ = vols;
            vols_ = new Matrix(vols.Count, vols[0].Count);

            checkInputs();
            initializeOptionDatesAndTimes();
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(volHandles_[i].Count == nStrikes_, () =>
                    i + 1 + " row of vol handles has size " +
                    volHandles_[i].Count + " instead of " + nStrikes_);
            }

            registerWithMarketData();
            for (var i = 0; i < vols_.rows(); ++i)
            for (var j = 0; j < vols_.columns(); ++j)
            {
                vols_[i, j] = volHandles_[i][j].link.value();
            }

            interpolate();
        }

        //! fixed reference date, floating market data
        public CapFloorTermVolSurface(Date settlementDate,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> strikes,
            List<List<Handle<Quote>>> vols,
            DayCounter dc = null)
            : base(settlementDate, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            nStrikes_ = strikes.Count;
            strikes_ = strikes;
            volHandles_ = vols;
            vols_ = new Matrix(vols.Count, vols[0].Count);

            checkInputs();
            initializeOptionDatesAndTimes();
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(volHandles_[i].Count == nStrikes_, () =>
                    i + 1 + " row of vol handles has size " + volHandles_[i].Count + " instead of " + nStrikes_);
            }

            registerWithMarketData();
            for (var i = 0; i < vols_.rows(); ++i)
            for (var j = 0; j < vols_.columns(); ++j)
            {
                vols_[i, j] = volHandles_[i][j].link.value();
            }

            interpolate();
        }

        //! fixed reference date, fixed market data
        public CapFloorTermVolSurface(Date settlementDate,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> strikes,
            Matrix vols,
            DayCounter dc = null)
            : base(settlementDate, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            nStrikes_ = strikes.Count;
            strikes_ = strikes;
            volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
            vols_ = vols;

            checkInputs();
            initializeOptionDatesAndTimes();
            // fill dummy handles to allow generic handle-based computations later
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                volHandles_[i] = new InitializedList<Handle<Quote>>(nStrikes_);
                for (var j = 0; j < nStrikes_; ++j)
                {
                    volHandles_[i][j] = new Handle<Quote>(new SimpleQuote(vols_[i, j]));
                }
            }

            interpolate();
        }

        //! floating reference date, fixed market data
        public CapFloorTermVolSurface(int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc,
            List<Period> optionTenors,
            List<double> strikes,
            Matrix vols,
            DayCounter dc = null)
            : base(settlementDays, calendar, bdc, dc ?? new Actual365Fixed())
        {
            nOptionTenors_ = optionTenors.Count;
            optionTenors_ = optionTenors;
            optionDates_ = new InitializedList<Date>(nOptionTenors_);
            optionTimes_ = new InitializedList<double>(nOptionTenors_);
            nStrikes_ = strikes.Count;
            strikes_ = strikes;
            volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
            vols_ = vols;

            checkInputs();
            initializeOptionDatesAndTimes();
            // fill dummy handles to allow generic handle-based computations later
            for (var i = 0; i < nOptionTenors_; ++i)
            {
                volHandles_[i] = new InitializedList<Handle<Quote>>(nStrikes_);
                for (var j = 0; j < nStrikes_; ++j)
                {
                    volHandles_[i][j] = new Handle<Quote>(new SimpleQuote(vols_[i, j]));
                }
            }

            interpolate();
        }

        // TermStructure interface
        public override Date maxDate()
        {
            calculate();
            return optionDateFromTenor(optionTenors_.Last());
        }

        public override double maxStrike() => strikes_.Last();

        // VolatilityTermStructure interface
        public override double minStrike() => strikes_.First();

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

        public List<double> strikes() => strikes_;

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

            for (var i = 0; i < nOptionTenors_; ++i)
            for (var j = 0; j < nStrikes_; ++j)
            {
                vols_[i, j] = volHandles_[i][j].link.value();
            }

            interpolation_.update();
        }

        protected override double volatilityImpl(double t, double strike)
        {
            calculate();
            return interpolation_.value(strike, t, true);
        }

        private void checkInputs()
        {
            QLNet.Utils.QL_REQUIRE(!optionTenors_.empty(), () => "empty option tenor vector");
            QLNet.Utils.QL_REQUIRE(nOptionTenors_ == vols_.rows(), () =>
                "mismatch between number of option tenors (" +
                nOptionTenors_ + ") and number of volatility rows (" +
                vols_.rows() + ")");
            QLNet.Utils.QL_REQUIRE(optionTenors_[0] > new Period(0, TimeUnit.Days), () =>
                "negative first option tenor: " + optionTenors_[0]);
            for (var i = 1; i < nOptionTenors_; ++i)
            {
                QLNet.Utils.QL_REQUIRE(optionTenors_[i] > optionTenors_[i - 1], () =>
                    "non increasing option tenor: " + i +
                    " is " + optionTenors_[i - 1] + ", " +
                    (i + 1) + " is " + optionTenors_[i]);
            }

            QLNet.Utils.QL_REQUIRE(nStrikes_ == vols_.columns(), () =>
                "mismatch between strikes(" + strikes_.Count +
                ") and vol columns (" + vols_.columns() + ")");
            for (var j = 1; j < nStrikes_; ++j)
            {
                QLNet.Utils.QL_REQUIRE(strikes_[j - 1] < strikes_[j], () =>
                    "non increasing strikes: " + j +
                    " is " + strikes_[j - 1] + ", " +
                    (j + 1) + " is " + strikes_[j]);
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
            interpolation_ = new BicubicSpline(strikes_, strikes_.Count, optionTimes_, optionTimes_.Count, vols_);
        }

        private void registerWithMarketData()
        {
            for (var i = 0; i < nOptionTenors_; ++i)
            for (var j = 0; j < nStrikes_; ++j)
            {
                volHandles_[i][j].registerWith(update);
            }
        }
    }
}
