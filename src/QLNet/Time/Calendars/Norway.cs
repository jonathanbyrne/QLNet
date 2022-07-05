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

using JetBrains.Annotations;

namespace QLNet.Time.Calendars
{
    //! Norwegian calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>Holy Thursday</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>Ascension</li>
        <li>Whit(Pentecost) Monday </li>
        <li>New Year's Day, January 1st</li>
        <li>May Day, May 1st</li>
        <li>National Independence Day, May 17st</li>
        <li>Christmas, December 25th</li>
        <li>Boxing Day, December 26th</li>
        </ul>

        \ingroup calendars
    */
    [PublicAPI]
    public class Norway : Calendar
    {
        private class Impl : WesternImpl
        {
            public static readonly Impl Singleton = new Impl();

            private Impl()
            {
            }

            public override bool isBusinessDay(Date date)
            {
                var w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                var m = (Month)date.Month;
                var y = date.Year;
                var em = easterMonday(y);

                if (isWeekend(w)
                    // Holy Thursday
                    || dd == em - 4
                    // Good Friday
                    || dd == em - 3
                    // Easter Monday
                    || dd == em
                    // Ascension Thursday
                    || dd == em + 38
                    // Whit Monday
                    || dd == em + 49
                    // New Year's Day
                    || d == 1 && m == Month.January
                    // May Day
                    || d == 1 && m == Month.May
                    // National Independence Day
                    || d == 17 && m == Month.May
                    // Christmas Eve
                    || d == 24 && m == Month.December && y >= 2002
                    // Christmas
                    || d == 25 && m == Month.December
                    // Boxing Day
                    || d == 26 && m == Month.December)
                {
                    return false;
                }

                return true;
            }

            public override string name() => "Norway";
        }

        public Norway() : base(Impl.Singleton)
        {
        }
    }
}
