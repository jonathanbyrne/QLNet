/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes
{
    //! base class for Inter-Bank-Offered-Rate indexes (e.g. %Libor, etc.)
    [PublicAPI]
    public class IborIndex : InterestRateIndex
    {
        protected BusinessDayConvention convention_;
        protected bool endOfMonth_;
        protected Handle<YieldTermStructure> termStructure_;

        public IborIndex(string familyName,
            Period tenor,
            int settlementDays,
            Currency currency,
            Calendar fixingCalendar,
            BusinessDayConvention convention,
            bool endOfMonth,
            DayCounter dayCounter,
            Handle<YieldTermStructure> h = null)
            : base(familyName, tenor, settlementDays, currency, fixingCalendar, dayCounter)
        {
            convention_ = convention;
            termStructure_ = h ?? new Handle<YieldTermStructure>();
            endOfMonth_ = endOfMonth;

            // observer interface
            if (!termStructure_.empty())
            {
                termStructure_.registerWith(update);
            }
        }

        // need by CashFlowVectors
        public IborIndex()
        {
        }

        // Inspectors
        public BusinessDayConvention businessDayConvention() => convention_;

        // Other methods
        // returns a copy of itself linked to a different forwarding curve
        public virtual IborIndex clone(Handle<YieldTermStructure> forwarding) =>
            new IborIndex(familyName(), tenor(), fixingDays(), currency(), fixingCalendar(),
                businessDayConvention(), endOfMonth(), dayCounter(), forwarding);

        public bool endOfMonth() => endOfMonth_;

        public override double forecastFixing(Date fixingDate)
        {
            var d1 = valueDate(fixingDate);
            var d2 = maturityDate(d1);
            var t = dayCounter_.yearFraction(d1, d2);
            QLNet.Utils.QL_REQUIRE(t > 0.0, () =>
                "\n cannot calculate forward rate between " +
                d1 + " and " + d2 +
                ":\n non positive time (" + t +
                ") using " + dayCounter_.name() + " daycounter");
            return forecastFixing(d1, d2, t);
        }

        public double forecastFixing(Date d1, Date d2, double t)
        {
            QLNet.Utils.QL_REQUIRE(!termStructure_.empty(), () => "null term structure set to this instance of " + name());
            var disc1 = termStructure_.link.discount(d1);
            var disc2 = termStructure_.link.discount(d2);
            return (disc1 / disc2 - 1.0) / t;
        }

        // the curve used to forecast fixings
        public Handle<YieldTermStructure> forwardingTermStructure() => termStructure_;

        // InterestRateIndex interface
        public override Date maturityDate(Date valueDate) => fixingCalendar().advance(valueDate, tenor_, convention_, endOfMonth_);
    }
}
