/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
    //! Base class for event
    //! This class acts as a base class for the actual event implementations.
    public abstract class Event : IObservable
    {
        #region Visitability

        public virtual void accept(IAcyclicVisitor v)
        {
            if (v != null)
            {
                v.visit(this);
            }
            else
            {
                QLNet.Utils.QL_FAIL("not an event visitor");
            }
        }

        #endregion

        #region Event interface

        //! returns the date at which the event occurs
        public abstract Date date();

        //! returns true if an event has already occurred before a date
        /*! If includeRefDate is true, then an event has not occurred if its
            date is the same as the refDate, i.e. this method returns false if
            the event date is the same as the refDate.
        */
        public virtual bool hasOccurred(Date d = null, bool? includeRefDate = null)
        {
            var refDate = d ?? Settings.evaluationDate();
            var includeRefDateEvent = includeRefDate ?? Settings.includeReferenceDateEvents;
            if (includeRefDateEvent)
            {
                return date() < refDate;
            }

            return date() <= refDate;
        }

        #endregion

        #region Observable interface

        private readonly WeakEventSource eventSource = new WeakEventSource();

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        #endregion
    }

    // used to create an Event instance.
    // to be replaced with specific events as soon as we find out which.
}
