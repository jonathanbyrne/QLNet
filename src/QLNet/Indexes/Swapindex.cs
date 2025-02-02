/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Instruments;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes
{
    //! base class for swap-rate indexes
    [PublicAPI]
    public class SwapIndex : InterestRateIndex
    {
        protected Handle<YieldTermStructure> discount_;
        protected bool exogenousDiscount_;
        protected BusinessDayConvention fixedLegConvention_;
        protected Period fixedLegTenor_;
        protected IborIndex iborIndex_;
        protected Date lastFixingDate_;
        // cache data to avoid swap recreation when the same fixing date
        // is used multiple time to forecast changing fixing
        protected VanillaSwap lastSwap_;
        protected new Period tenor_;

        // need by CashFlowVectors
        public SwapIndex()
        {
        }

        public SwapIndex(string familyName,
            Period tenor,
            int settlementDays,
            Currency currency,
            Calendar calendar,
            Period fixedLegTenor,
            BusinessDayConvention fixedLegConvention,
            DayCounter fixedLegDayCounter,
            IborIndex iborIndex) :
            base(familyName, tenor, settlementDays, currency, calendar, fixedLegDayCounter)
        {
            tenor_ = tenor;
            iborIndex_ = iborIndex;
            fixedLegTenor_ = fixedLegTenor;
            fixedLegConvention_ = fixedLegConvention;
            exogenousDiscount_ = false;
            discount_ = new Handle<YieldTermStructure>();

            iborIndex_.registerWith(update);
        }

        public SwapIndex(string familyName,
            Period tenor,
            int settlementDays,
            Currency currency,
            Calendar calendar,
            Period fixedLegTenor,
            BusinessDayConvention fixedLegConvention,
            DayCounter fixedLegDayCounter,
            IborIndex iborIndex,
            Handle<YieldTermStructure> discountingTermStructure) :
            base(familyName, tenor, settlementDays, currency, calendar, fixedLegDayCounter)
        {
            tenor_ = tenor;
            iborIndex_ = iborIndex;
            fixedLegTenor_ = fixedLegTenor;
            fixedLegConvention_ = fixedLegConvention;
            exogenousDiscount_ = true;
            discount_ = discountingTermStructure;

            iborIndex_.registerWith(update);
            discount_.registerWith(update);
        }

        // Other methods
        // returns a copy of itself linked to a different forwarding curve
        public virtual SwapIndex clone(Handle<YieldTermStructure> forwarding)
        {
            if (exogenousDiscount_)
            {
                return new SwapIndex(familyName(),
                    tenor(),
                    fixingDays(),
                    currency(),
                    fixingCalendar(),
                    fixedLegTenor(),
                    fixedLegConvention(),
                    dayCounter(),
                    iborIndex_.clone(forwarding),
                    discount_);
            }

            return new SwapIndex(familyName(),
                tenor(),
                fixingDays(),
                currency(),
                fixingCalendar(),
                fixedLegTenor(),
                fixedLegConvention(),
                dayCounter(),
                iborIndex_.clone(forwarding));
        }

        //! returns a copy of itself linked to a different curves
        public virtual SwapIndex clone(Handle<YieldTermStructure> forwarding, Handle<YieldTermStructure> discounting) =>
            new SwapIndex(familyName(),
                tenor(),
                fixingDays(),
                currency(),
                fixingCalendar(),
                fixedLegTenor(),
                fixedLegConvention(),
                dayCounter(),
                iborIndex_.clone(forwarding),
                discounting);

        //! returns a copy of itself linked to a different tenor
        public virtual SwapIndex clone(Period tenor)
        {
            if (exogenousDiscount_)
            {
                return new SwapIndex(familyName(),
                    tenor,
                    fixingDays(),
                    currency(),
                    fixingCalendar(),
                    fixedLegTenor(),
                    fixedLegConvention(),
                    dayCounter(),
                    iborIndex(),
                    discountingTermStructure());
            }

            return new SwapIndex(familyName(),
                tenor,
                fixingDays(),
                currency(),
                fixingCalendar(),
                fixedLegTenor(),
                fixedLegConvention(),
                dayCounter(),
                iborIndex());
        }

        public Handle<YieldTermStructure> discountingTermStructure() => discount_;

        public bool exogenousDiscount() => exogenousDiscount_;

        public BusinessDayConvention fixedLegConvention() => fixedLegConvention_;

        // Inspectors
        public Period fixedLegTenor() => fixedLegTenor_;

        public override double forecastFixing(Date fixingDate) => underlyingSwap(fixingDate).fairRate();

        public Handle<YieldTermStructure> forwardingTermStructure() => iborIndex_.forwardingTermStructure();

        public IborIndex iborIndex() => iborIndex_;

        // InterestRateIndex interface
        public override Date maturityDate(Date valueDate)
        {
            var fixDate = fixingDate(valueDate);
            return underlyingSwap(fixDate).maturityDate();
        }

        // \warning Relinking the term structure underlying the index will not have effect on the returned swap.
        // recheck
        public VanillaSwap underlyingSwap(Date fixingDate)
        {
            QLNet.Utils.QL_REQUIRE(fixingDate != null, () => "null fixing date");
            // caching mechanism
            if (lastFixingDate_ != fixingDate)
            {
                var fixedRate = 0.0;
                if (exogenousDiscount_)
                {
                    lastSwap_ = new MakeVanillaSwap(tenor_, iborIndex_, fixedRate)
                        .withEffectiveDate(valueDate(fixingDate))
                        .withFixedLegCalendar(fixingCalendar())
                        .withFixedLegDayCount(dayCounter_)
                        .withFixedLegTenor(fixedLegTenor_)
                        .withFixedLegConvention(fixedLegConvention_)
                        .withFixedLegTerminationDateConvention(fixedLegConvention_)
                        .withDiscountingTermStructure(discount_)
                        .value();
                }
                else
                {
                    lastSwap_ = new MakeVanillaSwap(tenor_, iborIndex_, fixedRate)
                        .withEffectiveDate(valueDate(fixingDate))
                        .withFixedLegCalendar(fixingCalendar())
                        .withFixedLegDayCount(dayCounter_)
                        .withFixedLegTenor(fixedLegTenor_)
                        .withFixedLegConvention(fixedLegConvention_)
                        .withFixedLegTerminationDateConvention(fixedLegConvention_)
                        .value();
                }

                lastFixingDate_ = fixingDate;
            }

            return lastSwap_;
        }
    }

    //! base class for overnight indexed swap indexes
}
