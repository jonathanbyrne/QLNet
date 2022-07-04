/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Patterns;
using QLNet.Termstructures;
using QLNet.Time;
using System;

namespace QLNet.Cashflows
{
    //! Base inflation-coupon pricer.
    /*! The main reason we can't use FloatingRateCouponPricer as the
        base is that it takes a FloatingRateCoupon which takes an
        InterestRateIndex and we need an inflation index (these are
        lagged).

        The basic inflation-specific thing that the pricer has to do
        is deal with different lags in the index and the option
        e.g. the option could look 3 months back and the index 2.

        We add the requirement that pricers do inverseCap/Floor-lets.
        These are cap/floor-lets as usually defined, i.e. pay out if
        underlying is above/below a strike.  The non-inverse (usual)
        versions are from a coupon point of view (a capped coupon has
        a maximum at the strike).

        We add the inverse prices so that conventional caps can be
        priced simply.
    */
    [JetBrains.Annotations.PublicAPI] public class InflationCouponPricer : IObserver, IObservable
    {
        // Interface
        public virtual double swapletPrice() => 0;

        public virtual double swapletRate() => 0;

        public virtual double capletPrice(double effectiveCap) => 0;

        public virtual double capletRate(double effectiveCap) => 0;

        public virtual double floorletPrice(double effectiveFloor) => 0;

        public virtual double floorletRate(double effectiveFloor) => 0;

        public virtual void initialize(InflationCoupon i) { }

        #region Observer & observable
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        // observer interface
        public void update() { notifyObservers(); }
        #endregion

        protected Handle<YieldTermStructure> rateCurve_;
        protected Date paymentDate_;

    }

    //! base pricer for capped/floored YoY inflation coupons
    /*! \note this pricer can already do swaplets but to get
              volatility-dependent coupons you need the descendents.
    */

    //! Black-formula pricer for capped/floored yoy inflation coupons

    //! Unit-Displaced-Black-formula pricer for capped/floored yoy inflation coupons

    //! Bachelier-formula pricer for capped/floored yoy inflation coupons
}
