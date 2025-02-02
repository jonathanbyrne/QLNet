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

namespace QLNet.Time
{
    //! Main cycle of the International %Money Market (a.k.a. %IMM) months
    public struct IMM
    {
        private enum Month
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

        // returns the IMM code for the given date (e.g. H3 for March 20th, 2013).
        public static string code(Date immDate)
        {
            if (!isIMMdate(immDate, false))
            {
                throw new ArgumentException(immDate + " is not an IMM date");
            }

            return "FGHJKMNQUVXZ"[immDate.Month - 1] + (immDate.Year % 10).ToString();
        }

        // returns the IMM date for the given IMM code (e.g. March 20th, 2013 for H3).
        public static Date date(string immCode) => date(immCode, null);

        public static Date date(string immCode, Date refDate)
        {
            if (!isIMMcode(immCode, false))
            {
                throw new ArgumentException(immCode + " is not a valid IMM code");
            }

            var referenceDate = refDate ?? Settings.evaluationDate();

            var m = "FGHJKMNQUVXZ".IndexOf(immCode.ToUpper()[0]) + 1;
            if (m == 0)
            {
                throw new ArgumentException("invalid IMM month letter");
            }

            if (!char.IsDigit(immCode[1]))
            {
                throw new ArgumentException(immCode + " is not a valid IMM code");
            }

            var y = immCode[1] - '0';

            y += referenceDate.Year - referenceDate.Year % 10;

            /* year<10 are not valid years: to avoid a run-time
               exception in few lines below we need to add 10 years right away */
            if (y == 0 && referenceDate.Year <= 1909)
            {
                y += 10;
            }

            var result = nextDate(new Date(1, m, y), false);
            if (result < referenceDate)
            {
                result = nextDate(new Date(1, m, y + 10), false);
            }

            return result;
        }

        //! returns whether or not the given string is an IMM code
        public static bool isIMMcode(string s) => isIMMcode(s, true);

        public static bool isIMMcode(string s, bool mainCycle)
        {
            if (s.Length != 2)
            {
                return false;
            }

            var str1 = "0123456789";
            if (!str1.Contains(s[1].ToString()))
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

            if (!str1.Contains(s[0].ToString()))
            {
                return false;
            }

            return true;
        }

        //! returns whether or not the given date is an IMM date
        public static bool isIMMdate(Date date) => isIMMdate(date, true);

        public static bool isIMMdate(Date date, bool mainCycle)
        {
            if (date.DayOfWeek != DayOfWeek.Wednesday)
            {
                return false;
            }

            if (date.Day < 15 || date.Day > 21)
            {
                return false;
            }

            if (!mainCycle)
            {
                return true;
            }

            switch ((QLNet.Month)date.Month)
            {
                case QLNet.Month.March:
                case QLNet.Month.June:
                case QLNet.Month.September:
                case QLNet.Month.December:
                    return true;
                default:
                    return false;
            }
        }

        /*! returns the IMM code for next contract listed in the
            International Money Market section of the Chicago Mercantile Exchange.*/
        public static string nextCode() => nextCode((Date)null, true);

        public static string nextCode(Date d) => nextCode(d, true);

        public static string nextCode(Date d, bool mainCycle)
        {
            var date = nextDate(d, mainCycle);
            return code(date);
        }

        /*! returns the IMM code for next contract listed in the
            International Money Market section of the Chicago Mercantile Exchange. */
        public static string nextCode(string immCode) => nextCode(immCode, true, null);

        public static string nextCode(string immCode, bool mainCycle) => nextCode(immCode, mainCycle, null);

        public static string nextCode(string immCode, bool mainCycle, Date referenceDate)
        {
            var date = nextDate(immCode, mainCycle, referenceDate);
            return code(date);
        }

        //! next IMM date following the given date
        /*! returns the 1st delivery date for next contract listed in the
            International Money Market section of the Chicago Mercantile Exchange. */
        public static Date nextDate() => nextDate((Date)null, true);

        public static Date nextDate(Date d) => nextDate(d, true);

        public static Date nextDate(Date date, bool mainCycle)
        {
            var refDate = date ?? Settings.evaluationDate();

            var y = refDate.Year;
            var m = refDate.Month;

            var offset = mainCycle ? 3 : 1;
            var skipMonths = offset - m % offset;
            if (skipMonths != offset || refDate.Day > 21)
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

            var result = Date.nthWeekday(3, DayOfWeek.Wednesday, m, y);
            if (result <= refDate)
            {
                result = nextDate(new Date(22, m, y), mainCycle);
            }

            return result;
        }

        //! next IMM date following the given IMM code
        /*! returns the 1st delivery date for next contract listed in the
            International Money Market section of the Chicago Mercantile Exchange. */
        public static Date nextDate(string immCode) => nextDate(immCode, true, null);

        public static Date nextDate(string immCode, bool mainCycle) => nextDate(immCode, mainCycle, null);

        public static Date nextDate(string immCode, bool mainCycle, Date referenceDate)
        {
            var immDate = date(immCode, referenceDate);
            return nextDate(immDate + 1, mainCycle);
        }
    }
}
