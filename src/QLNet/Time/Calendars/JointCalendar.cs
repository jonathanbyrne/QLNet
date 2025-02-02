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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace QLNet.Time.Calendars
{
    [PublicAPI]
    public class JointCalendar : Calendar
    {
        //! rules for joining calendars
        public enum JointCalendarRule
        {
            JoinHolidays, /*!< A date is a holiday for the joint calendar if it is a holiday
                                  for any of the given calendars */
            JoinBusinessDays /*!< A date is a business day for the joint calendar if it is a business day
                                  for any of the given calendars */
        }

        private class Impl : Calendar
        {
            private readonly List<Calendar> calendars_ = new List<Calendar>();
            private readonly JointCalendarRule rule_;

            public Impl(Calendar c1, Calendar c2, JointCalendarRule r)
            {
                rule_ = r;
                calendars_.Add(c1);
                calendars_.Add(c2);
            }

            public Impl(Calendar c1, Calendar c2, Calendar c3, JointCalendarRule r)
            {
                rule_ = r;
                calendars_.Add(c1);
                calendars_.Add(c2);
                calendars_.Add(c3);
            }

            public Impl(Calendar c1, Calendar c2, Calendar c3, Calendar c4, JointCalendarRule r)
            {
                rule_ = r;
                calendars_.Add(c1);
                calendars_.Add(c2);
                calendars_.Add(c3);
                calendars_.Add(c4);
            }

            public override bool isBusinessDay(Date date)
            {
                switch (rule_)
                {
                    case JointCalendarRule.JoinHolidays:
                        foreach (var c in calendars_)
                        {
                            if (c.isHoliday(date))
                            {
                                return false;
                            }
                        }

                        return true;
                    case JointCalendarRule.JoinBusinessDays:
                        foreach (var c in calendars_)
                        {
                            if (c.isBusinessDay(date))
                            {
                                return true;
                            }
                        }

                        return false;
                    default:
                        QLNet.Utils.QL_FAIL("unknown joint calendar rule");
                        return false;
                }
            }

            public override bool isWeekend(DayOfWeek w)
            {
                switch (rule_)
                {
                    case JointCalendarRule.JoinHolidays:
                        foreach (var c in calendars_)
                        {
                            if (c.isWeekend(w))
                            {
                                return true;
                            }
                        }

                        return false;
                    case JointCalendarRule.JoinBusinessDays:
                        foreach (var c in calendars_)
                        {
                            if (c.isWeekend(w))
                            {
                                return false;
                            }
                        }

                        return true;
                    default:
                        QLNet.Utils.QL_FAIL("unknown joint calendar rule");
                        return false;
                }
            }

            public override string name()
            {
                var result = "";
                switch (rule_)
                {
                    case JointCalendarRule.JoinHolidays:
                        result += "JoinHolidays(";
                        break;
                    case JointCalendarRule.JoinBusinessDays:
                        result += "JoinBusinessDays(";
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown joint calendar rule");
                        break;
                }

                result += calendars_.First().name();
                for (var i = 1; i < calendars_.Count; i++)
                {
                    result += ", " + calendars_[i].name();
                }

                result += ")";
                return result;
            }
        }

        //! Joint calendar
        /*! Depending on the chosen rule, this calendar has a set of business days given by either the union or the intersection
            of the sets of business days of the given calendars.
            \test the correctness of the returned results is tested by reproducing the calculations. */
        public JointCalendar(Calendar c1, Calendar c2)
            : this(c1, c2, JointCalendarRule.JoinHolidays)
        {
        }

        public JointCalendar(Calendar c1, Calendar c2, JointCalendarRule r)
            : base(new Impl(c1, c2, r))
        {
        }

        public JointCalendar(Calendar c1, Calendar c2, Calendar c3)
            : this(c1, c2, c3, JointCalendarRule.JoinHolidays)
        {
        }

        public JointCalendar(Calendar c1, Calendar c2, Calendar c3, JointCalendarRule r)
            : base(new Impl(c1, c2, c3, r))
        {
        }

        public JointCalendar(Calendar c1, Calendar c2, Calendar c3, Calendar c4)
            : this(c1, c2, c3, c4, JointCalendarRule.JoinHolidays)
        {
        }

        public JointCalendar(Calendar c1, Calendar c2, Calendar c3, Calendar c4, JointCalendarRule r)
            : base(new Impl(c1, c2, c3, c4, r))
        {
        }
    }
}
