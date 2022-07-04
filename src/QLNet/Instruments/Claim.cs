﻿/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;

namespace QLNet.Instruments
{
    public abstract class Claim : IObservable, IObserver
    {
        #region Observer & Observable

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

        public void update()
        {
            notifyObservers();
        }

        #endregion Observer & Observable

        public abstract double amount(Date defaultDate, double notional, double recoveryRate);
    }

    //! Claim on a notional

    //! Claim on the notional of a reference security, including accrual
}
