/*
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
using JetBrains.Annotations;

namespace QLNet.Time.Calendars
{
    //! United States calendars
    /*! Public holidays (see: http://www.opm.gov/fedhol/):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday, or to Friday if on Saturday)</li>
        <li>Martin Luther King's birthday, third Monday in January (since 1983)</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Columbus Day, second Monday in October</li>
        <li>Veterans' Day, November 11th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        </ul>

        Holidays for the stock exchange (data from http://www.nyse.com):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Martin Luther King's birthday, third Monday in January (since
            1998)</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Good Friday</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Presidential election day, first Tuesday in November of election
            years (until 1980)</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        <li>Special historic closings (see
            http://www.nyse.com/pdfs/closings.pdf)</li>
        </ul>

        Holidays for the government bond market (data from
        http://www.bondmarkets.com):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Martin Luther King's birthday, third Monday in January</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Good Friday</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Columbus Day, second Monday in October</li>
        <li>Veterans' Day, November 11th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        </ul>

        Holidays for the North American Energy Reliability Council
        (data from http://www.nerc.com/~oc/offpeaks.html):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday)</li>
        </ul>

        \test the correctness of the returned results is tested
              against a list of known holidays.
    */

    [PublicAPI]
    public class UnitedStates : Calendar
    {
        //! US calendars
        public enum Market
        {
            Settlement, //!< generic settlement calendar
            NYSE, //!< New York stock exchange calendar
            GovernmentBond, //!< government-bond calendar
            NERC, //!< off-peak days for NERC
            LiborImpact, //!< Libor impact calendar
            FederalReserve //!< Federal Reserve Bankwire System
        }

        private class FederalReserve : WesternImpl
        {
            public static readonly FederalReserve Singleton = new FederalReserve();

            private FederalReserve()
            {
            }

            public override bool isBusinessDay(Date date)
            {
                // see https://www.frbservices.org/holidayschedules/ for details
                var w = date.DayOfWeek;
                var d = date.Day;
                var m = (Month)date.Month;
                var y = date.year();
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || (d == 1 || d == 2 && w == DayOfWeek.Monday) && m == Month.January
                    // Martin Luther King's birthday (third Monday in January)
                    || d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.January
                    && y >= 1983
                    // Washington's birthday (third Monday in February)
                    || isWashingtonBirthday(d, m, y, w)
                    // Memorial Day (last Monday in May)
                    || isMemorialDay(d, m, y, w)
                    // Juneteenth (Monday if Sunday or Friday if Saturday)
                    || isJuneteenth(d, m, y, w)
                    // Independence Day (Monday if Sunday)
                    || (d == 4 || d == 5 && w == DayOfWeek.Monday) && m == Month.July
                    // Labor Day (first Monday in September)
                    || isLaborDay(d, m, y, w)
                    // Columbus Day (second Monday in October)
                    || isColumbusDay(d, m, y, w)
                    // Veteran's Day (Monday if Sunday)
                    || isVeteransDayNoSaturday(d, m, y, w)
                    // Thanksgiving Day (fourth Thursday in November)
                    || d >= 22 && d <= 28 && w == DayOfWeek.Thursday && m == Month.November
                    // Christmas (Monday if Sunday)
                    || (d == 25 || d == 26 && w == DayOfWeek.Monday) && m == Month.December)
                {
                    return false;
                }

                return true;
            }

            public override string name() => "Federal Reserve Bankwire System";
        }

        private class GovernmentBond : WesternImpl
        {
            public static readonly GovernmentBond Singleton = new GovernmentBond();

            private GovernmentBond()
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
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || (d == 1 || d == 2 && w == DayOfWeek.Monday) && m == Month.January
                    // Martin Luther King's birthday (third Monday in January)
                    || d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.January && y >= 1983
                    // Washington's birthday (third Monday in February)
                    || isWashingtonBirthday(d, m, y, w)
                    // Good Friday (2015 was half day due to NFP report)
                    || dd == em - 3 && y != 2015
                    // Memorial Day (last Monday in May)
                    || isMemorialDay(d, m, y, w)
                    // Juneteenth (Monday if Sunday or Friday if Saturday)
                    || isJuneteenth(d, m, y, w)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || (d == 4 || d == 5 && w == DayOfWeek.Monday ||
                        d == 3 && w == DayOfWeek.Friday) && m == Month.July
                    // Labor Day (first Monday in September)
                    || isLaborDay(d, m, y, w)
                    // Columbus Day (second Monday in October)
                    || isColumbusDay(d, m, y, w)
                    // Veteran's Day (Monday if Sunday)
                    || isVeteransDayNoSaturday(d, m, y, w)
                    // Thanksgiving Day (fourth Thursday in November)
                    || d >= 22 && d <= 28 && w == DayOfWeek.Thursday && m == Month.November
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || (d == 25 || d == 26 && w == DayOfWeek.Monday ||
                        d == 24 && w == DayOfWeek.Friday) && m == Month.December)
                {
                    return false;
                }

                // Special closings
                if ( // President Bush's Funeral
                    y == 2018 && m == Month.December && d == 5
                    // Hurricane Sandy
                    || y == 2012 && m == Month.October && d == 30
                    // President Reagan's funeral
                    || y == 2004 && m == Month.June && d == 11
                   )
                {
                    return false;
                }

                return true;
            }

            public override string name() => "US government bond market";
        }

        private class LiborImpact : Settlement
        {
            public new static readonly LiborImpact Singleton = new LiborImpact();

            private LiborImpact()
            {
            }

            public override bool isBusinessDay(Date date)
            {
                // Since 2015 Independence Day only impacts Libor if it falls
                // on a weekday
                var w = date.DayOfWeek;
                var d = date.Day;
                var m = (Month)date.Month;
                var y = date.year();
                if ((d == 5 && w == DayOfWeek.Monday ||
                     d == 3 && w == DayOfWeek.Friday) && m == Month.July && y >= 2015)
                {
                    return true;
                }

                return base.isBusinessDay(date);
            }

            public override string name() => "US with Libor impact";
        }

        private class NERC : WesternImpl
        {
            public static readonly NERC Singleton = new NERC();

            private NERC()
            {
            }

            public override bool isBusinessDay(Date date)
            {
                var w = date.DayOfWeek;
                var d = date.Day;
                var m = (Month)date.Month;
                var y = date.Year;
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || (d == 1 || d == 2 && w == DayOfWeek.Monday) && m == Month.January
                    // Memorial Day (last Monday in May)
                    || isMemorialDay(d, m, y, w)
                    // Independence Day (Monday if Sunday)
                    || (d == 4 || d == 5 && w == DayOfWeek.Monday) && m == Month.July
                    // Labor Day (first Monday in September)
                    || isLaborDay(d, m, y, w)
                    // Thanksgiving Day (fourth Thursday in November)
                    || d >= 22 && d <= 28 && w == DayOfWeek.Thursday && m == Month.November
                    // Christmas (Monday if Sunday)
                    || (d == 25 || d == 26 && w == DayOfWeek.Monday) && m == Month.December)
                {
                    return false;
                }

                return true;
            }

            public override string name() => "North American Energy Reliability Council";
        }

        private class NYSE : WesternImpl
        {
            public static readonly NYSE Singleton = new NYSE();

            private NYSE()
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
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || (d == 1 || d == 2 && w == DayOfWeek.Monday) && m == Month.January
                    // Washington's birthday (third Monday in February)
                    || isWashingtonBirthday(d, m, y, w)
                    // Good Friday
                    || dd == em - 3
                    // Memorial Day (last Monday in May)
                    || isMemorialDay(d, m, y, w)
                    // Juneteenth (Monday if Sunday or Friday if Saturday)
                    || isJuneteenth(d, m, y, w)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || (d == 4 || d == 5 && w == DayOfWeek.Monday ||
                        d == 3 && w == DayOfWeek.Friday) && m == Month.July
                    // Labor Day (first Monday in September)
                    || isLaborDay(d, m, y, w)
                    // Thanksgiving Day (fourth Thursday in November)
                    || d >= 22 && d <= 28 && w == DayOfWeek.Thursday && m == Month.November
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || (d == 25 || d == 26 && w == DayOfWeek.Monday ||
                        d == 24 && w == DayOfWeek.Friday) && m == Month.December
                   )
                {
                    return false;
                }

                if (y >= 1998 && d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.January)
                    // Martin Luther King's birthday (third Monday in January)
                {
                    return false;
                }

                if ((y <= 1968 || y <= 1980 && y % 4 == 0) && m == Month.November
                                                           && d <= 7 && w == DayOfWeek.Tuesday)
                    // Presidential election days
                {
                    return false;
                }

                // Special closings
                if ( // President Bush's Funeral
                    y == 2018 && m == Month.December && d == 5
                    // Hurricane Sandy
                    || y == 2012 && m == Month.October && (d == 29 || d == 30)
                    // President Ford's funeral
                    || y == 2007 && m == Month.January && d == 2
                    // President Reagan's funeral
                    || y == 2004 && m == Month.June && d == 11
                    // September 11-14, 2001
                    || y == 2001 && m == Month.September && 11 <= d && d <= 14
                    // President Nixon's funeral
                    || y == 1994 && m == Month.April && d == 27
                    // Hurricane Gloria
                    || y == 1985 && m == Month.September && d == 27
                    // 1977 Blackout
                    || y == 1977 && m == Month.July && d == 14
                    // Funeral of former President Lyndon B. Johnson.
                    || y == 1973 && m == Month.January && d == 25
                    // Funeral of former President Harry S. Truman
                    || y == 1972 && m == Month.December && d == 28
                    // National Day of Participation for the lunar exploration.
                    || y == 1969 && m == Month.July && d == 21
                    // Funeral of former President Eisenhower.
                    || y == 1969 && m == Month.March && d == 31
                    // Closed all day - heavy snow.
                    || y == 1969 && m == Month.February && d == 10
                    // Day after Independence Day.
                    || y == 1968 && m == Month.July && d == 5
                    // June 12-Dec. 31, 1968
                    // Four day week (closed on Wednesdays) - Paperwork Crisis
                    || y == 1968 && dd >= 163 && w == DayOfWeek.Wednesday
                    // Day of mourning for Martin Luther King Jr.
                    || y == 1968 && m == Month.April && d == 9
                    // Funeral of President Kennedy
                    || y == 1963 && m == Month.November && d == 25
                    // Day before Decoration Day
                    || y == 1961 && m == Month.May && d == 29
                    // Day after Christmas
                    || y == 1958 && m == Month.December && d == 26
                    // Christmas Eve
                    || (y == 1954 || y == 1956 || y == 1965)
                    && m == Month.December && d == 24
                   )
                {
                    return false;
                }

                return true;
            }

            public override string name() => "New York stock exchange";
        }

        private class Settlement : WesternImpl
        {
            public static readonly Settlement Singleton = new Settlement();

            protected Settlement()
            {
            }

            public override bool isBusinessDay(Date date)
            {
                var w = date.DayOfWeek;
                var d = date.Day;
                var m = (Month)date.Month;
                var y = date.Year;
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || (d == 1 || d == 2 && w == DayOfWeek.Monday) && m == Month.January
                    // (or to Friday if on Saturday)
                    || d == 31 && w == DayOfWeek.Friday && m == Month.December
                    // Martin Luther King's birthday (third Monday in January)
                    || d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.January && y >= 1983
                    // Washington's birthday (third Monday in February)
                    || isWashingtonBirthday(d, m, y, w)
                    // Memorial Day (last Monday in May)
                    || isMemorialDay(d, m, y, w)
                    // Juneteenth (Monday if Sunday or Friday if Saturday)
                    || isJuneteenth(d, m, y, w)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || (d == 4 || d == 5 && w == DayOfWeek.Monday ||
                        d == 3 && w == DayOfWeek.Friday) && m == Month.July
                    // Labor Day (first Monday in September)
                    || isLaborDay(d, m, y, w)
                    // Columbus Day (second Monday in October)
                    || isColumbusDay(d, m, y, w)
                    // Veteran's Day (Monday if Sunday or Friday if Saturday)
                    || isVeteransDay(d, m, y, w)
                    // Thanksgiving Day (fourth Thursday in November)
                    || d >= 22 && d <= 28 && w == DayOfWeek.Thursday && m == Month.November
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || (d == 25 || d == 26 && w == DayOfWeek.Monday ||
                        d == 24 && w == DayOfWeek.Friday) && m == Month.December)
                {
                    return false;
                }

                return true;
            }

            public override string name() => "US settlement";
        }

        public UnitedStates() : this(Market.Settlement)
        {
        }

        public UnitedStates(Market m)
        {
            switch (m)
            {
                case Market.Settlement:
                    calendar_ = Settlement.Singleton;
                    break;
                case Market.NYSE:
                    calendar_ = NYSE.Singleton;
                    break;
                case Market.GovernmentBond:
                    calendar_ = GovernmentBond.Singleton;
                    break;
                case Market.NERC:
                    calendar_ = NERC.Singleton;
                    break;
                case Market.LiborImpact:
                    calendar_ = LiborImpact.Singleton;
                    break;
                case Market.FederalReserve:
                    calendar_ = FederalReserve.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m);
            }
        }

        protected static bool isColumbusDay(int d, Month m, int y, DayOfWeek w) =>
            // second Monday in October
            d >= 8 && d <= 14 && w == DayOfWeek.Monday && m == Month.October
            && y >= 1971;

        protected static bool isJuneteenth(int d, Month m, int y, DayOfWeek w) =>
            // declared in 2021, but only observed by exchanges since 2022
            (d == 19 || d == 20 && w == DayOfWeek.Monday || d == 18 && w == DayOfWeek.Friday)
            && m == Month.June && y >= 2022;

        protected static bool isLaborDay(int d, Month m, int y, DayOfWeek w) =>
            // first Monday in September
            d <= 7 && w == DayOfWeek.Monday && m == Month.September;

        protected static bool isMemorialDay(int d, Month m, int y, DayOfWeek w)
        {
            if (y >= 1971)
            {
                // last Monday in May
                return d >= 25 && w == DayOfWeek.Monday && m == Month.May;
            }

            // May 30th, possibly adjusted
            return (d == 30 || d == 31 && w == DayOfWeek.Monday
                            || d == 29 && w == DayOfWeek.Friday) && m == Month.May;
        }

        protected static bool isVeteransDay(int d, Month m, int y, DayOfWeek w)
        {
            if (y <= 1970 || y >= 1978)
            {
                // November 11th, adjusted
                return (d == 11 || d == 12 && w == DayOfWeek.Monday ||
                        d == 10 && w == DayOfWeek.Friday) && m == Month.November;
            }

            // fourth Monday in October
            return d >= 22 && d <= 28 && w == DayOfWeek.Monday && m == Month.October;
        }

        protected static bool isVeteransDayNoSaturday(int d, Month m, int y, DayOfWeek w)
        {
            if (y <= 1970 || y >= 1978)
            {
                // November 11th, adjusted, but no Saturday to Friday
                return (d == 11 || d == 12 && w == DayOfWeek.Monday) && m == Month.November;
            }

            // fourth Monday in October
            return d >= 22 && d <= 28 && w == DayOfWeek.Monday && m == Month.October;
        }

        // a few rules used by multiple calendars
        protected static bool isWashingtonBirthday(int d, Month m, int y, DayOfWeek w)
        {
            if (y >= 1971)
            {
                // third Monday in February
                return d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.February;
            }

            // February 22nd, possily adjusted
            return (d == 22 || d == 23 && w == DayOfWeek.Monday
                            || d == 21 && w == DayOfWeek.Friday) && m == Month.February;
        }
    }
}
