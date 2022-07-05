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

using JetBrains.Annotations;

namespace QLNet.Time
{
    // This class provides methods for determining the length of a time period according to given market convention,
    // both as a number of days and as a year fraction.
    [PublicAPI]
    public class DayCounter
    {
        // this is a placeholder for actual day counters for Singleton pattern use
        protected DayCounter dayCounter_;

        // constructors
        /*! The default constructor returns a day counter with a null implementation, which is therefore unusable except as a
            placeholder. */
        public DayCounter()
        {
        }

        public DayCounter(DayCounter d)
        {
            dayCounter_ = d;
        }

        public DayCounter dayCounter
        {
            get => dayCounter_;
            set => dayCounter_ = value;
        }

        // comparison based on name
        // Returns <tt>true</tt> iff the two day counters belong to the same derived class.
        public static bool operator ==(DayCounter d1, DayCounter d2) =>
            (object)d1 == null || (object)d2 == null ? (object)d1 == null && (object)d2 == null : d1.empty() && d2.empty() || !d1.empty() && !d2.empty() && d1.name() == d2.name();

        public static bool operator !=(DayCounter d1, DayCounter d2) => !(d1 == d2);

        public virtual int dayCount(Date d1, Date d2)
        {
            QLNet.Utils.QL_REQUIRE(!empty(), () => "No implementation provided");
            return dayCounter_.dayCount(d1, d2);
        }

        public bool empty() => dayCounter_ == null;

        public override bool Equals(object o) => this == (DayCounter)o;

        public override int GetHashCode() => 0;

        public virtual string name()
        {
            if (empty())
            {
                return "No implementation provided";
            }

            return dayCounter_.name();
        }

        public override string ToString() => name();

        public double yearFraction(Date d1, Date d2) => yearFraction(d1, d2, d1, d2);

        public virtual double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
        {
            QLNet.Utils.QL_REQUIRE(!empty(), () => "No implementation provided");
            return dayCounter_.yearFraction(d1, d2, refPeriodStart, refPeriodEnd);
        }
    }
}
