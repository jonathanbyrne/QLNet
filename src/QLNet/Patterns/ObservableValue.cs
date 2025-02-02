﻿/*
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

namespace QLNet.Patterns
{
    //! %observable and assignable proxy to concrete value
    /*! Observers can be registered with instances of this class so
        that they are notified when a different value is assigned to
        such instances. Client code can copy the contained value or
        pass it to functions via implicit conversion.
        \note it is not possible to call non-const method on the
              returned value. This is by design, as this possibility
              would necessarily bypass the notification code; client
              code should modify the value via re-assignment instead.
    */
    [PublicAPI]
    public class ObservableValue<T> : IObservable where T : new()
    {
        // Subjects, i.e. observables, should define interface internally like follows.
        private readonly WeakEventSource eventSource = new WeakEventSource();
        private T value_;

        public ObservableValue()
        {
            value_ = FastActivator<T>.Create();
        }

        public ObservableValue(T t)
        {
            value_ = t;
        }

        public ObservableValue(ObservableValue<T> t)
        {
            value_ = t.value_;
        }

        // controlled assignment
        public ObservableValue<T> Assign(T t)
        {
            value_ = t;
            notifyObservers();
            return this;
        }

        public ObservableValue<T> Assign(ObservableValue<T> t)
        {
            value_ = t.value_;
            notifyObservers();
            return this;
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        //! explicit inspector
        public T value() => value_;

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }
    }
}
