﻿/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Time.DayCounters
{
    //! Actual/365 (No Leap) day count convention
    /*! "Actual/365 (No Leap)" day count convention, also known as
       "Act/365 (NL)", "NL/365", or "Actual/365 (JGB)".

       \ingroup daycounters
    */
    [PublicAPI]
    public class Actual365NoLeap : DayCounter
    {
        private class Impl : DayCounter
        {
            public static readonly Impl Singleton = new Impl();
            private static readonly int[] MonthOffset =
            {
                0, 31, 59, 90, 120, 151, // Jan - Jun
                181, 212, 243, 273, 304, 334 // Jun - Dec
            };

            private Impl()
            {
            }

            public override int dayCount(Date d1, Date d2)
            {
                int s1, s2;

                s1 = d1.Day + MonthOffset[d1.month() - 1] + d1.year() * 365;
                s2 = d2.Day + MonthOffset[d2.month() - 1] + d2.year() * 365;

                if (d1.month() == (int)Month.Feb && d1.Day == 29)
                {
                    --s1;
                }

                if (d2.month() == (int)Month.Feb && d2.Day == 29)
                {
                    --s2;
                }

                return s2 - s1;
            }

            public override string name() => "Actual/365 (NL)";

            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd) => dayCount(d1, d2) / 365.0;
        }

        public Actual365NoLeap() : base(Impl.Singleton)
        {
        }
    }
}
