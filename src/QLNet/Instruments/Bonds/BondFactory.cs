﻿/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;
using QLNet.Time.Calendars;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class BondFactory
    {
        public static AmortizingBond makeAmortizingBond(double FaceValue,
            double MarketValue,
            double CouponRate,
            Date IssueDate,
            Date MaturityDate,
            Date TradeDate,
            Frequency payFrequency,
            DayCounter dCounter,
            AmortizingMethod Method,
            double gYield = 0) =>
            new AmortizingBond(FaceValue,
                MarketValue,
                CouponRate,
                IssueDate,
                MaturityDate,
                TradeDate,
                payFrequency,
                dCounter,
                Method,
                new NullCalendar(), gYield);

        public static AmortizingFixedRateBond makeAmortizingFixedBond(Date startDate,
            Period bondLength,
            DayCounter dCounter,
            Frequency payFrequency,
            double amount,
            double rate) =>
            makeAmortizingFixedBond(startDate, bondLength, dCounter, payFrequency, amount, rate, new TARGET());

        public static AmortizingFixedRateBond makeAmortizingFixedBond(Date startDate,
            Period bondLength,
            DayCounter dCounter,
            Frequency payFrequency,
            double amount,
            double rate,
            Calendar calendar)
        {
            AmortizingFixedRateBond bond;
            var endDate = calendar.advance(startDate, bondLength);

            var schedule = new Schedule(startDate, endDate, bondLength, calendar, BusinessDayConvention.Unadjusted,
                BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

            bond = new AmortizingFixedRateBond(0, calendar, amount, startDate, bondLength, payFrequency, rate, dCounter);

            return bond;
        }

        public static MBSFixedRateBond makeMBSFixedBond(Date startDate,
            Period bondLength,
            Period originalLength,
            DayCounter dCounter,
            Frequency payFrequency,
            double amount,
            double WACRate,
            double PassThroughRate,
            PSACurve psaCurve) =>
            makeMBSFixedBond(startDate, bondLength, originalLength, dCounter, payFrequency, amount, WACRate, PassThroughRate, psaCurve, new TARGET());

        public static MBSFixedRateBond makeMBSFixedBond(Date startDate,
            Period bondLength,
            Period originalLength,
            DayCounter dCounter,
            Frequency payFrequency,
            double amount,
            double WACrate,
            double PassThroughRate,
            PSACurve psaCurve,
            Calendar calendar)
        {
            MBSFixedRateBond bond;
            var endDate = calendar.advance(startDate, bondLength);

            var schedule = new Schedule(startDate, endDate, bondLength, calendar, BusinessDayConvention.Unadjusted,
                BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

            bond = new MBSFixedRateBond(0, calendar, amount, startDate, bondLength, originalLength, payFrequency, WACrate, PassThroughRate, dCounter, psaCurve);

            return bond;
        }
    }
}
