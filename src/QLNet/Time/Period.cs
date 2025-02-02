/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Time
{
    [PublicAPI]
    public class Period : IComparable
    {
        // required by operator <
        private struct pair
        {
            public readonly int lo;
            public readonly int hi;

            public pair(Period p)
            {
                switch (p.units())
                {
                    case TimeUnit.Days:
                        lo = hi = p.length();
                        break;
                    case TimeUnit.Weeks:
                        lo = hi = 7 * p.length();
                        break;
                    case TimeUnit.Months:
                        lo = 28 * p.length();
                        hi = 31 * p.length();
                        break;
                    case TimeUnit.Years:
                        lo = 365 * p.length();
                        hi = 366 * p.length();
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("Unknown TimeUnit: " + p.units());
                        lo = hi = 0;
                        break;
                }
            }
        }

        private int length_;
        private TimeUnit unit_;

        public Period()
        {
            length_ = 0;
            unit_ = TimeUnit.Days;
        }

        public Period(int n, TimeUnit u)
        {
            length_ = n;
            unit_ = u;
        }

        public Period(Frequency f)
        {
            switch (f)
            {
                case Frequency.NoFrequency:
                    unit_ = TimeUnit.Days; // same as Period()
                    length_ = 0;
                    break;
                case Frequency.Once:
                    unit_ = TimeUnit.Years;
                    length_ = 0;
                    break;
                case Frequency.Annual:
                    unit_ = TimeUnit.Years;
                    length_ = 1;
                    break;
                case Frequency.Semiannual:
                case Frequency.EveryFourthMonth:
                case Frequency.Quarterly:
                case Frequency.Bimonthly:
                case Frequency.Monthly:
                    unit_ = TimeUnit.Months;
                    length_ = 12 / (int)f;
                    break;
                case Frequency.EveryFourthWeek:
                case Frequency.Biweekly:
                case Frequency.Weekly:
                    unit_ = TimeUnit.Weeks;
                    length_ = 52 / (int)f;
                    break;
                case Frequency.Daily:
                    unit_ = TimeUnit.Days;
                    length_ = 1;
                    break;
                case Frequency.OtherFrequency:
                    QLNet.Utils.QL_FAIL("unknown frequency");
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown frequency (" + f + ")");
                    break;
            }
        }

        // Create from a string like "1M", "2Y"...
        public Period(string periodString)
        {
            periodString = periodString.ToUpper();
            length_ = int.Parse(periodString.Substring(0, periodString.Length - 1));
            var freq = periodString.Substring(periodString.Length - 1, 1);
            switch (freq)
            {
                case "D":
                    unit_ = TimeUnit.Days;
                    break;
                case "W":
                    unit_ = TimeUnit.Weeks;
                    break;
                case "M":
                    unit_ = TimeUnit.Months;
                    break;
                case "Y":
                    unit_ = TimeUnit.Years;
                    break;
                default:
                    throw new ArgumentException("Unknown TimeUnit: " + freq);
            }
        }

        public static Period operator +(Period p1, Period p2)
        {
            var length_ = p1.length();
            var units_ = p1.units();

            if (length_ == 0)
            {
                length_ = p2.length();
                units_ = p2.units();
            }
            else if (units_ == p2.units())
            {
                // no conversion needed
                length_ += p2.length();
            }
            else
            {
                switch (units_)
                {
                    case TimeUnit.Years:
                        switch (p2.units())
                        {
                            case TimeUnit.Months:
                                units_ = TimeUnit.Months;
                                length_ = length_ * 12 + p2.length();
                                break;
                            case TimeUnit.Weeks:
                            case TimeUnit.Days:
                                QLNet.Utils.QL_REQUIRE(p1.length() == 0, () => "impossible addition between " + p1 + " and " + p2);
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unknown time unit (" + p2.units() + ")");
                                break;
                        }

                        break;

                    case TimeUnit.Months:
                        switch (p2.units())
                        {
                            case TimeUnit.Years:
                                length_ += p2.length() * 12;
                                break;
                            case TimeUnit.Weeks:
                            case TimeUnit.Days:
                                QLNet.Utils.QL_REQUIRE(p1.length() == 0, () => "impossible addition between " + p1 + " and " + p2);
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unknown time unit (" + p2.units() + ")");
                                break;
                        }

                        break;

                    case TimeUnit.Weeks:
                        switch (p2.units())
                        {
                            case TimeUnit.Days:
                                units_ = TimeUnit.Days;
                                length_ = length_ * 7 + p2.length();
                                break;
                            case TimeUnit.Years:
                            case TimeUnit.Months:
                                QLNet.Utils.QL_REQUIRE(p1.length() == 0, () => "impossible addition between " + p1 + " and " + p2);
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unknown time unit (" + p2.units() + ")");
                                break;
                        }

                        break;

                    case TimeUnit.Days:
                        switch (p2.units())
                        {
                            case TimeUnit.Weeks:
                                length_ += p2.length() * 7;
                                break;
                            case TimeUnit.Years:
                            case TimeUnit.Months:
                                QLNet.Utils.QL_REQUIRE(p1.length() == 0, () => "impossible addition between " + p1 + " and " + p2);
                                break;
                            default:
                                QLNet.Utils.QL_FAIL("unknown time unit (" + p2.units() + ")");
                                break;
                        }

                        break;

                    default:
                        QLNet.Utils.QL_FAIL("unknown time unit (" + units_ + ")");
                        break;
                }
            }

            return new Period(length_, units_);
        }

        public static bool operator ==(Period p1, Period p2)
        {
            if ((object)p1 == null && (object)p2 == null)
            {
                return true;
            }

            if ((object)p1 == null || (object)p2 == null)
            {
                return false;
            }

            return !(p1 < p2 || p2 < p1);
        }

        public static bool operator >(Period p1, Period p2) => p2 < p1;

        public static bool operator >=(Period p1, Period p2) => !(p1 < p2);

        public static bool operator !=(Period p1, Period p2) => !(p1 == p2);

        public static bool operator <(Period p1, Period p2)
        {
            // special cases
            if (p1.length() == 0)
            {
                return p2.length() > 0;
            }

            if (p2.length() == 0)
            {
                return p1.length() < 0;
            }

            // exact comparisons
            if (p1.units() == p2.units())
            {
                return p1.length() < p2.length();
            }

            if (p1.units() == TimeUnit.Months && p2.units() == TimeUnit.Years)
            {
                return p1.length() < 12 * p2.length();
            }

            if (p1.units() == TimeUnit.Years && p2.units() == TimeUnit.Months)
            {
                return 12 * p1.length() < p2.length();
            }

            if (p1.units() == TimeUnit.Days && p2.units() == TimeUnit.Weeks)
            {
                return p1.length() < 7 * p2.length();
            }

            if (p1.units() == TimeUnit.Weeks && p2.units() == TimeUnit.Days)
            {
                return 7 * p1.length() < p2.length();
            }

            // inexact comparisons (handled by converting to days and using limits)
            pair p1lim = new pair(p1), p2lim = new pair(p2);
            if (p1lim.hi < p2lim.lo || p2lim.hi < p1lim.lo)
            {
                return p1lim.hi < p2lim.lo;
            }

            QLNet.Utils.QL_FAIL("Undecidable comparison between " + p1 + " and " + p2);
            return false;
        }

        public static bool operator <=(Period p1, Period p2) => !(p1 > p2);

        public static Period operator *(int n, Period p) => new Period(n * p.length(), p.units());

        public static Period operator *(Period p, int n) => new Period(n * p.length(), p.units());

        public static Period operator -(Period p1, Period p2) => p1 + -p2;

        public static Period operator -(Period p) => new Period(-p.length(), p.units());

        public int CompareTo(object obj)
        {
            if (this < (Period)obj)
            {
                return -1;
            }

            if (this == (Period)obj)
            {
                return 0;
            }

            return 1;
        }

        public override bool Equals(object o) => this == (Period)o;

        public Frequency frequency()
        {
            var length = System.Math.Abs(length_); // unsigned version

            if (length == 0)
            {
                if (unit_ == TimeUnit.Years)
                {
                    return Frequency.Once;
                }

                return Frequency.NoFrequency;
            }

            switch (unit_)
            {
                case TimeUnit.Years:
                    return length == 1 ? Frequency.Annual : Frequency.OtherFrequency;
                case TimeUnit.Months:
                    if (12 % length == 0 && length <= 12)
                    {
                        return (Frequency)(12 / length);
                    }

                    return Frequency.OtherFrequency;
                case TimeUnit.Weeks:
                    if (length == 1)
                    {
                        return Frequency.Weekly;
                    }

                    if (length == 2)
                    {
                        return Frequency.Biweekly;
                    }

                    if (length == 4)
                    {
                        return Frequency.EveryFourthWeek;
                    }

                    return Frequency.OtherFrequency;
                case TimeUnit.Days:
                    return length == 1 ? Frequency.Daily : Frequency.OtherFrequency;
                default:
                    throw new ArgumentException("Unknown TimeUnit: " + unit_);
            }
        }

        public override int GetHashCode() => 0;

        // properties
        public int length() => length_;

        public void normalize()
        {
            if (length_ != 0)
            {
                switch (unit_)
                {
                    case TimeUnit.Days:
                        if (length_ % 7 == 0)
                        {
                            length_ /= 7;
                            unit_ = TimeUnit.Weeks;
                        }

                        break;
                    case TimeUnit.Months:
                        if (length_ % 12 == 0)
                        {
                            length_ /= 12;
                            unit_ = TimeUnit.Years;
                        }

                        break;
                    case TimeUnit.Weeks:
                    case TimeUnit.Years:
                        break;
                    default:
                        throw new ArgumentException("Unknown TimeUnit: " + unit_);
                }
            }
        }

        public string ToShortString()
        {
            var result = "";
            var n = length();
            var m = 0;
            switch (units())
            {
                case TimeUnit.Days:
                    if (n >= 7)
                    {
                        m = n / 7;
                        result += m + "W";
                        n = n % 7;
                    }

                    if (n != 0 || m == 0)
                    {
                        return result + n + "D";
                    }

                    return result;
                case TimeUnit.Weeks:
                    return result + n + "W";
                case TimeUnit.Months:
                    if (n >= 12)
                    {
                        m = n / 12;
                        result += n / 12 + "Y";
                        n = n % 12;
                    }

                    if (n != 0 || m == 0)
                    {
                        return result + n + "M";
                    }

                    return result;
                case TimeUnit.Years:
                    return result + n + "Y";
                default:
                    QLNet.Utils.QL_FAIL("unknown time unit (" + units() + ")");
                    return result;
            }
        }

        public override string ToString() => "TimeUnit: " + unit_ + ", length: " + length_;

        public TimeUnit units() => unit_;
    }
}
