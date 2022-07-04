/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

namespace QLNet.Time.Calendars
{
    //! Polish calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>Easter Monday</li>
        <li>Corpus Christi</li>
        <li>New Year's Day, January 1st</li>
        <li>Epiphany, January 6th (since 2011)</li>
        <li>May Day, May 1st</li>
        <li>Constitution Day, May 3rd</li>
        <li>Assumption of the Blessed Virgin Mary, August 15th</li>
        <li>All Saints Day, November 1st</li>
        <li>Independence Day, November 11th</li>
        <li>Christmas, December 25th</li>
        <li>2nd Day of Christmas, December 26th</li>
        </ul>

        \ingroup calendars
    */
    [JetBrains.Annotations.PublicAPI] public class Poland : Calendar
    {
        public Poland() : base(Impl.Singleton) { }

        class Impl : WesternImpl
        {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() => "Poland";

            public override bool isBusinessDay(Date date)
            {
                var w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                var m = (Month)date.Month;
                var y = date.Year;
                var em = easterMonday(y);

                if (isWeekend(w)
                    // Easter Monday
                    || dd == em
                    // Corpus Christi
                    || dd == em + 59
                    // New Year's Day
                    || d == 1 && m == Month.January
                    // Epiphany
                    || d == 6 && m == Month.January && y >= 2011
                    // May Day
                    || d == 1 && m == Month.May
                    // Constitution Day
                    || d == 3 && m == Month.May
                    // Assumption of the Blessed Virgin Mary
                    || d == 15 && m == Month.August
                    // All Saints Day
                    || d == 1 && m == Month.November
                    // Independence Day
                    || d == 11 && m == Month.November
                    // Christmas
                    || d == 25 && m == Month.December
                    // 2nd Day of Christmas
                    || d == 26 && m == Month.December)
                    return false;
                return true;
            }
        }
    }
}

