﻿/*
 Copyright (C) 2008-2016 Andrea Maggiulli

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

namespace QLNet.Time
{
    public static class ASX
    {
        //! Main cycle of the Australian Securities Exchange (a.k.a. ASX) months

        private enum Months
        {
            F = 1,
            G = 2,
            H = 3,
            J = 4,
            K = 5,
            M = 6,
            N = 7,
            Q = 8,
            U = 9,
            V = 10,
            X = 11,
            Z = 12
        }

        /*! returns the ASX code for the given date
           (e.g. M5 for June 12th, 2015).
        */
        public static string code(Date date)
        {
            QLNet.Utils.QL_REQUIRE(isASXdate(date, false), () => date + " is not an ASX date");

            var ASXcode = string.Empty;
            var y = (date.year() % 10).ToString();
            switch ((Month)date.month())
            {
                case Month.January:
                    ASXcode = 'F' + y;
                    break;
                case Month.February:
                    ASXcode = 'G' + y;
                    break;
                case Month.March:
                    ASXcode = 'H' + y;
                    break;
                case Month.April:
                    ASXcode = 'J' + y;
                    break;
                case Month.May:
                    ASXcode = 'K' + y;
                    break;
                case Month.June:
                    ASXcode = 'M' + y;
                    break;
                case Month.July:
                    ASXcode = 'N' + y;
                    break;
                case Month.August:
                    ASXcode = 'Q' + y;
                    break;
                case Month.September:
                    ASXcode = 'U' + y;
                    break;
                case Month.October:
                    ASXcode = 'V' + y;
                    break;
                case Month.November:
                    ASXcode = 'X' + y;
                    break;
                case Month.December:
                    ASXcode = 'Z' + y;
                    break;
                default:
                    QLNet.Utils.QL_FAIL("not an ASX month (and it should have been)");
                    break;
            }

            return ASXcode;
        }

        /*! returns the ASX date for the given ASX code
           (e.g. June 12th, 2015 for M5).

           \warning It raises an exception if the input
                    string is not an ASX code
        */
        public static Date date(string asxCode, Date refDate = null)
        {
            QLNet.Utils.QL_REQUIRE(isASXcode(asxCode, false), () =>
                asxCode + " is not a valid ASX code");

            var referenceDate = refDate ?? Settings.evaluationDate();

            var code = asxCode.ToUpper();
            var ms = code.Substring(0, 1);
            Month m = 0;
            if (ms == "F")
            {
                m = Month.January;
            }
            else if (ms == "G")
            {
                m = Month.February;
            }
            else if (ms == "H")
            {
                m = Month.March;
            }
            else if (ms == "J")
            {
                m = Month.April;
            }
            else if (ms == "K")
            {
                m = Month.May;
            }
            else if (ms == "M")
            {
                m = Month.June;
            }
            else if (ms == "N")
            {
                m = Month.July;
            }
            else if (ms == "Q")
            {
                m = Month.August;
            }
            else if (ms == "U")
            {
                m = Month.September;
            }
            else if (ms == "V")
            {
                m = Month.October;
            }
            else if (ms == "X")
            {
                m = Month.November;
            }
            else if (ms == "Z")
            {
                m = Month.December;
            }
            else
            {
                QLNet.Utils.QL_FAIL("invalid ASX month letter");
            }

            //       Year y = boost::lexical_cast<Year>(); // lexical_cast causes compilation errors with x64

            var y = int.Parse(code.Substring(1, 1));
            /* year<1900 are not valid QuantLib years: to avoid a run-time
              exception few lines below we need to add 10 years right away */
            if (y == 0 && referenceDate.year() <= 1909)
            {
                y += 10;
            }

            var referenceYear = referenceDate.year() % 10;
            y += referenceDate.year() - referenceYear;
            var result = nextDate(new Date(1, m, y), false);
            if (result < referenceDate)
            {
                return nextDate(new Date(1, m, y + 10), false);
            }

            return result;
        }

        //! returns whether or not the given string is an ASX code
        public static bool isASXcode(string inString, bool mainCycle = true)
        {
            if (inString.Length != 2)
            {
                return false;
            }

            var str1 = "0123456789";
            var loc = str1.Contains(inString.Substring(1, 1));
            if (!loc)
            {
                return false;
            }

            if (mainCycle)
            {
                str1 = "hmzuHMZU";
            }
            else
            {
                str1 = "fghjkmnquvxzFGHJKMNQUVXZ";
            }

            loc = str1.Contains(inString.Substring(0, 1));
            if (!loc)
            {
                return false;
            }

            return true;
        }

        //! returns whether or not the given date is an ASX date
        public static bool isASXdate(Date date, bool mainCycle = true)
        {
            if (date.weekday() != (int)DayOfWeek.Friday + 1)
            {
                return false;
            }

            var d = date.Day;
            if (d < 8 || d > 14)
            {
                return false;
            }

            if (!mainCycle)
            {
                return true;
            }

            switch ((Month)date.month())
            {
                case Month.March:
                case Month.June:
                case Month.September:
                case Month.December:
                    return true;
                default:
                    return false;
            }
        }

        //! next ASX code following the given date
        /*! returns the ASX code for next contract listed in the
           Australian Securities Exchange
        */
        public static string nextCode(Date d = null, bool mainCycle = true)
        {
            var date = nextDate(d, mainCycle);
            return code(date);
        }

        //! next ASX code following the given code
        /*! returns the ASX code for next contract listed in the
           Australian Securities Exchange
        */
        public static string nextCode(string asxCode, bool mainCycle = true, Date referenceDate = null)
        {
            var date = nextDate(asxCode, mainCycle, referenceDate);
            return code(date);
        }

        //! next ASX date following the given date
        /*! returns the 1st delivery date for next contract listed in the
           Australian Securities Exchange.
        */
        public static Date nextDate(Date date = null, bool mainCycle = true)
        {
            var refDate = date ?? Settings.evaluationDate();
            var y = refDate.year();
            var m = refDate.month();

            var offset = mainCycle ? 3 : 1;
            var skipMonths = offset - m % offset;
            if (skipMonths != offset || refDate.Day > 14)
            {
                skipMonths += m;
                if (skipMonths <= 12)
                {
                    m = skipMonths;
                }
                else
                {
                    m = skipMonths - 12;
                    y += 1;
                }
            }

            var result = Date.nthWeekday(2, DayOfWeek.Friday, m, y);
            if (result <= refDate)
            {
                result = nextDate(new Date(15, m, y), mainCycle);
            }

            return result;
        }

        //! next ASX date following the given ASX code
        /*! returns the 1st delivery date for next contract listed in the
           Australian Securities Exchange
        */
        public static Date nextDate(string ASXcode, bool mainCycle = true, Date referenceDate = null)
        {
            var asxDate = date(ASXcode, referenceDate);
            return nextDate(asxDate + 1, mainCycle);
        }
    }
}
