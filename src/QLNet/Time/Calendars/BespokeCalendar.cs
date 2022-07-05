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
using JetBrains.Annotations;

namespace QLNet.Time.Calendars
{
    //! Bespoke calendar
    /*! This calendar has no predefined set of business days. Holidays
        and weekdays can be defined by means of the provided
        interface. Instances constructed by copying remain linked to
        the original one; adding a new holiday or weekday will affect
        all linked instances.

        \ingroup calendars
    */
    [PublicAPI]
    public class BespokeCalendar : Calendar
    {
        // here implementation does not follow a singleton pattern
        private class Impl : WesternImpl
        {
            private readonly List<DayOfWeek> weekend_ = new List<DayOfWeek>();

            public void addWeekend(DayOfWeek w)
            {
                weekend_.Add(w);
            }

            public override bool isBusinessDay(Date date) => !isWeekend(date.DayOfWeek);

            public override bool isWeekend(DayOfWeek w) => weekend_.Contains(w);
        }

        private string name_;

        /*! \warning different bespoke calendars created with the same
                     name (or different bespoke calendars created with
                     no name) will compare as equal.
        */
        public BespokeCalendar() : this("")
        {
        }

        public BespokeCalendar(string name) : base(new Impl())
        {
            name_ = name;
        }

        //! marks the passed day as part of the weekend
        public void addWeekend(DayOfWeek w)
        {
            var impl = calendar_ as Impl;
            if (impl != null)
            {
                impl.addWeekend(w);
            }
        }

        public override string name() => name_;
    }
}
