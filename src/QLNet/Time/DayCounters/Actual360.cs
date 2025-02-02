/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2017  Andrea Maggiulli (a.maggiulli@gmail.com)

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
    //! Actual/360 day count convention
    /*! Actual/360 day count convention, also known as "Act/360", or "A/360". */
    [PublicAPI]
    public class Actual360 : DayCounter
    {
        public enum Actual360Convention
        {
            excludeLastDay,
            includeLastDay
        }

        private class Impl : DayCounter
        {
            public static readonly Impl Singleton = new Impl();

            private Impl()
            {
            }

            public override int dayCount(Date d1, Date d2) => d2 - d1;

            public override string name() => "Actual/360";

            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd) => Date.daysBetween(d1, d2) / 360.0;
        }

        private class IncludedImpl : DayCounter
        {
            public static readonly IncludedImpl Singleton = new IncludedImpl();

            private IncludedImpl()
            {
            }

            public override int dayCount(Date d1, Date d2) => d2 - d1 + 1;

            public override string name() => "Actual/360 (inc)";

            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd) => (Date.daysBetween(d1, d2) + 1) / 360.0;
        }

        public Actual360(bool c = false) : base(conventions(c))
        {
        }

        private static DayCounter conventions(bool c)
        {
            if (c)
            {
                return IncludedImpl.Singleton;
            }

            return Impl.Singleton;
        }
    }
}
