﻿/*
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

using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Credit
{
    //! Flat hazard-rate curve
    /*! \ingroup defaultprobabilitytermstructures */
    [PublicAPI]
    public class FlatHazardRate : HazardRateStructure
    {
        private Handle<Quote> hazardRate_;

        #region TermStructure interface

        public override Date maxDate() => Date.maxDate();

        #endregion

        #region HazardRateStructure interface

        protected override double hazardRateImpl(double t) => hazardRate_.link.value();

        #endregion

        #region DefaultProbabilityTermStructure interface

        protected override double survivalProbabilityImpl(double t) => System.Math.Exp(-hazardRate_.link.value() * t);

        #endregion

        #region Constructors

        public FlatHazardRate(Date referenceDate, Handle<Quote> hazardRate, DayCounter dc)
            : base(referenceDate, new Calendar(), dc)
        {
            hazardRate_ = hazardRate;
            hazardRate_.registerWith(update);
        }

        public FlatHazardRate(Date referenceDate, double hazardRate, DayCounter dc)
            : base(referenceDate, new Calendar(), dc)
        {
            hazardRate_ = new Handle<Quote>(new SimpleQuote(hazardRate));
        }

        public FlatHazardRate(int settlementDays, Calendar calendar, Handle<Quote> hazardRate, DayCounter dc)
            : base(settlementDays, calendar, dc)
        {
            hazardRate_ = hazardRate;
            hazardRate_.registerWith(update);
        }

        public FlatHazardRate(int settlementDays, Calendar calendar, double hazardRate, DayCounter dc)
            : base(settlementDays, calendar, dc)
        {
            hazardRate_ = new Handle<Quote>(new SimpleQuote(hazardRate));
        }

        #endregion
    }
}
