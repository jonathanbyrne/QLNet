/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Patterns;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class FloatingRateCoupon : Coupon, IObserver
    {
        protected DayCounter dayCounter_;
        protected int fixingDays_;
        protected double gearing_;
        protected InterestRateIndex index_;
        protected bool isInArrears_;
        protected FloatingRateCouponPricer pricer_;
        protected double spread_;

        // constructors
        public FloatingRateCoupon(Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            InterestRateIndex index,
            double gearing = 1.0,
            double spread = 0.0,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            DayCounter dayCounter = null,
            bool isInArrears = false)
            : base(paymentDate, nominal, startDate, endDate, refPeriodStart, refPeriodEnd)
        {
            index_ = index;
            dayCounter_ = dayCounter ?? new DayCounter();
            fixingDays_ = fixingDays == default ? index.fixingDays() : fixingDays;
            gearing_ = gearing;
            spread_ = spread;
            isInArrears_ = isInArrears;

            if (gearing_.IsEqual(0))
            {
                throw new ArgumentException("Null gearing not allowed");
            }

            if (dayCounter_.empty())
            {
                dayCounter_ = index_.dayCounter();
            }

            // add as observer
            index_.registerWith(update);
            Settings.registerWith(update);
        }

        // need by CashFlowVectors
        public FloatingRateCoupon()
        {
        }

        //! convexity-adjusted fixing
        public double adjustedFixing => (rate() - spread()) / gearing();

        public int fixingDays => fixingDays_; //! fixing days

        public override double accruedAmount(Date d)
        {
            if (d <= accrualStartDate_ || d > paymentDate_)
            {
                return 0;
            }

            return nominal() * rate() *
                   dayCounter().yearFraction(accrualStartDate_, Date.Min(d, accrualEndDate_), refPeriodStart_, refPeriodEnd_);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // CashFlow interface
        public override double amount()
        {
            var result = rate() * accrualPeriod() * nominal();
            return result;
        }

        //! convexity adjustment
        public virtual double convexityAdjustment() => convexityAdjustmentImpl(indexFixing());

        public override DayCounter dayCounter() => dayCounter_;

        // Factory - for Leg generators
        public virtual CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays,
            InterestRateIndex index, double gearing, double spread,
            Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears) =>
            new FloatingRateCoupon(paymentDate, nominal, startDate, endDate, fixingDays,
                index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);

        public virtual Date fixingDate()
        {
            //! fixing date
            // if isInArrears_ fix at the end of period
            var refDate = isInArrears_ ? accrualEndDate_ : accrualStartDate_;
            return index_.fixingCalendar().advance(refDate, -fixingDays_, TimeUnit.Days, BusinessDayConvention.Preceding);
        }

        public double gearing() => gearing_; //! index gearing, i.e. multiplicative coefficient for the index

        //////////////////////////////////////////////////////////////////////////////////////
        // properties
        public InterestRateIndex index() => index_; //! floating index

        //! fixing of the underlying index
        public virtual double indexFixing() => index_.fixing(fixingDate());

        //! whether or not the coupon fixes in arrears
        public bool isInArrears() => isInArrears_;

        //////////////////////////////////////////////////////////////////////////////////////
        // methods
        public double price(YieldTermStructure yts) => amount() * yts.discount(date());

        public FloatingRateCouponPricer pricer() => pricer_;

        //////////////////////////////////////////////////////////////////////////////////////
        // Coupon interface
        public override double rate()
        {
            if (pricer_ == null)
            {
                throw new ArgumentException("pricer not set");
            }

            pricer_.initialize(this);
            var result = pricer_.swapletRate();
            return result;
        }

        public virtual void setPricer(FloatingRateCouponPricer pricer)
        {
            if (pricer_ != null) // remove from the old observable
            {
                pricer_.unregisterWith(update);
            }

            pricer_ = pricer;

            if (pricer_ != null)
            {
                pricer_.registerWith(update); // add to observers of new pricer
            }

            update(); // fire the change event to notify observers of this
        }

        public double spread() => spread_; //! spread paid over the fixing of the underlying index

        // Observer interface
        public void update()
        {
            notifyObservers();
        }

        //! convexity adjustment for the given index fixing
        protected double convexityAdjustmentImpl(double f) => gearing().IsEqual(0.0) ? 0.0 : adjustedFixing - f;
    }
}
