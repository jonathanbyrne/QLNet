/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 *
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
using QLNet.Indexes;
using QLNet.Patterns;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Cashflows
{
    //! Base inflation-coupon class
    /*! The day counter is usually obtained from the inflation term
        structure that the inflation index uses for forecasting.
        There is no gearing or spread because these are relevant for
        YoY coupons but not zero inflation coupons.

        \note inflation indices do not contain day counters or calendars.
    */
    [PublicAPI]
    public class InflationCoupon : Coupon, IObserver
    {
        protected DayCounter dayCounter_;
        protected int fixingDays_;
        protected InflationIndex index_;
        protected Period observationLag_;
        protected InflationCouponPricer pricer_;

        public InflationCoupon(Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            InflationIndex index,
            Period observationLag,
            DayCounter dayCounter,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            Date exCouponDate = null)
            : base(paymentDate, nominal, startDate, endDate, refPeriodStart, refPeriodEnd, exCouponDate) // ref period is before lag
        {
            index_ = index;
            observationLag_ = observationLag;
            dayCounter_ = dayCounter;
            fixingDays_ = fixingDays;

            index_.registerWith(update);
            Settings.registerWith(update);
        }

        public override double accruedAmount(Date d)
        {
            if (d <= accrualStartDate_ || d > paymentDate_)
            {
                return 0.0;
            }

            return nominal() * rate() *
                   dayCounter().yearFraction(accrualStartDate_,
                       d < accrualEndDate_ ? d : accrualEndDate_, //System.Math.Min(d, accrualEndDate_),
                       refPeriodStart_,
                       refPeriodEnd_);
        }

        // CashFlow interface
        public override double amount() => rate() * accrualPeriod() * nominal();

        public override DayCounter dayCounter() => dayCounter_;

        //! fixing date
        public virtual Date fixingDate() =>
            // fixing calendar is usually the null calendar for inflation indices
            index_.fixingCalendar().advance(refPeriodEnd_ - observationLag_,
                -fixingDays_, TimeUnit.Days, BusinessDayConvention.ModifiedPreceding);

        //! fixing days
        public int fixingDays() => fixingDays_;

        // Inspectors
        //! yoy inflation index
        public InflationIndex index() => index_;

        //! fixing of the underlying index, as observed by the coupon
        public virtual double indexFixing() => index_.fixing(fixingDate());

        //! how the coupon observes the index
        public Period observationLag() => observationLag_;

        // Coupon interface
        public double price(Handle<YieldTermStructure> discountingCurve) => amount() * discountingCurve.link.discount(date());

        public InflationCouponPricer pricer() => pricer_;

        public override double rate()
        {
            QLNet.Utils.QL_REQUIRE(pricer_ != null, () => "pricer not set");

            // we know it is the correct ExerciseType because checkPricerImpl checks on setting
            // in general pricer_ will be a derived class, as will *this on calling
            pricer_.initialize(this);
            return pricer_.swapletRate();
        }

        public void setPricer(InflationCouponPricer pricer)
        {
            QLNet.Utils.QL_REQUIRE(checkPricerImpl(pricer), () => "pricer given is wrong ExerciseType");

            if (pricer_ != null)
            {
                pricer_.unregisterWith(update);
            }

            pricer_ = pricer;

            if (pricer_ != null)
            {
                pricer_.registerWith(update);
            }

            update();
        }

        public void update()
        {
            notifyObservers();
        }

        //! makes sure you were given the correct ExerciseType of pricer
        // this can also done in external pricer setter classes via
        // accept/visit mechanism
        protected virtual bool checkPricerImpl(InflationCouponPricer i) => false;
    }
}
