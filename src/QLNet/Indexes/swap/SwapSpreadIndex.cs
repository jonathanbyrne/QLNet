﻿//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Indexes.swap
{
    /// <summary>
    ///     class for swap-rate spread indexes
    /// </summary>
    [PublicAPI]
    public class SwapSpreadIndex : InterestRateIndex
    {
        private double gearing1_, gearing2_;
        private SwapIndex swapIndex1_, swapIndex2_;

        public SwapSpreadIndex(string familyName,
            SwapIndex swapIndex1,
            SwapIndex swapIndex2,
            double gearing1 = 1.0,
            double gearing2 = -1.0)
            : base(familyName, swapIndex1.tenor(), // does not make sense, but we have to provide one
                swapIndex1.fixingDays(), swapIndex1.currency(), swapIndex1.fixingCalendar(), swapIndex1.dayCounter())
        {
            swapIndex1_ = swapIndex1;
            swapIndex2_ = swapIndex2;
            gearing1_ = gearing1;
            gearing2_ = gearing2;

            swapIndex1_.registerWith(update);
            swapIndex2_.registerWith(update);

            name_ = swapIndex1_.name() + "(" + gearing1 + ") + "
                    + swapIndex2_.name() + "(" + gearing1 + ")";

            QLNet.Utils.QL_REQUIRE(swapIndex1_.fixingDays() == swapIndex2_.fixingDays(), () =>
                "index1 fixing days ("
                + swapIndex1_.fixingDays() + ")"
                + "must be equal to index2 fixing days ("
                + swapIndex2_.fixingDays() + ")");

            QLNet.Utils.QL_REQUIRE(swapIndex1_.fixingCalendar() == swapIndex2_.fixingCalendar(), () =>
                "index1 fixingCalendar ("
                + swapIndex1_.fixingCalendar() + ")"
                + "must be equal to index2 fixingCalendar ("
                + swapIndex2_.fixingCalendar() + ")");

            QLNet.Utils.QL_REQUIRE(swapIndex1_.currency() == swapIndex2_.currency(), () =>
                "index1 currency (" + swapIndex1_.currency() + ")"
                + "must be equal to index2 currency ("
                + swapIndex2_.currency() + ")");

            QLNet.Utils.QL_REQUIRE(swapIndex1_.dayCounter() == swapIndex2_.dayCounter(), () =>
                "index1 dayCounter ("
                + swapIndex1_.dayCounter() + ")"
                + "must be equal to index2 dayCounter ("
                + swapIndex2_.dayCounter() + ")");

            QLNet.Utils.QL_REQUIRE(swapIndex1_.fixedLegTenor() == swapIndex2_.fixedLegTenor(), () =>
                "index1 fixedLegTenor ("
                + swapIndex1_.fixedLegTenor() + ")"
                + "must be equal to index2 fixedLegTenor ("
                + swapIndex2_.fixedLegTenor());

            QLNet.Utils.QL_REQUIRE(swapIndex1_.fixedLegConvention() == swapIndex2_.fixedLegConvention(), () =>
                "index1 fixedLegConvention ("
                + swapIndex1_.fixedLegConvention() + ")"
                + "must be equal to index2 fixedLegConvention ("
                + swapIndex2_.fixedLegConvention());
        }

        // need by CashFlowVectors
        public SwapSpreadIndex()
        {
        }

        public override bool allowsNativeFixings() => false;

        public override double forecastFixing(Date fixingDate) =>
            gearing1_ * swapIndex1_.fixing(fixingDate) +
            gearing2_ * swapIndex2_.fixing(fixingDate);

        public double gearing1() => gearing1_;

        public double gearing2() => gearing2_;

        // InterestRateIndex interface
        public override Date maturityDate(Date valueDate)
        {
            QLNet.Utils.QL_FAIL("SwapSpreadIndex does not provide a single maturity date");
            return null;
        }

        public override double? pastFixing(Date fixingDate) =>
            gearing1_ * swapIndex1_.pastFixing(fixingDate) +
            gearing2_ * swapIndex2_.pastFixing(fixingDate);

        //! \name Inspectors
        public SwapIndex swapIndex1() => swapIndex1_;

        public SwapIndex swapIndex2() => swapIndex2_;
    }
}
