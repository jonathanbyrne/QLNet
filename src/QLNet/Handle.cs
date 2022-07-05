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
using QLNet.Patterns;

namespace QLNet
{
    //! Shared handle to an observable
    /*! All copies of an instance of this class refer to the same observable by means of a relinkable smart pointer. When such
        pointer is relinked to another observable, the change will be propagated to all the copies.
        <tt>registerAsObserver</tt> is not needed since C# does automatic garbage collection */

    [PublicAPI]
    public class Handle<T> where T : IObservable
    {
        protected class Link : IObservable, IObserver
        {
            // Observable
            private readonly WeakEventSource eventSource = new WeakEventSource();
            private T h_;
            private bool isObserver_;

            public Link(T h, bool registerAsObserver)
            {
                linkTo(h, registerAsObserver);
            }

            public T currentLink() => h_;

            public bool empty() => h_ == null;

            public void linkTo(T h, bool registerAsObserver)
            {
                if (h != null && (!h.Equals(h_) || (isObserver_ != registerAsObserver)))
                {
                    if (h_ != null && isObserver_)
                    {
                        h_.unregisterWith(update);
                    }

                    h_ = h;
                    isObserver_ = registerAsObserver;

                    if (isObserver_)
                    {
                        h_.registerWith(update);
                    }

                    // finally, notify observers of this of the change in the underlying object
                    notifyObservers();
                }
            }

            public void registerWith(Callback handler)
            {
                notifyObserversEvent += handler;
            }

            public void unregisterWith(Callback handler)
            {
                notifyObserversEvent -= handler;
            }

            public void update()
            {
                notifyObservers();
            }

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

        protected Link link_;

        public Handle() : this(default(T))
        {
        }

        public Handle(T h) : this(h, true)
        {
        }

        public Handle(T h, bool registerAsObserver)
        {
            link_ = new Link(h, registerAsObserver);
        }

        public T link
        {
            get
            {
                Utils.QL_REQUIRE(!empty(), () => "empty Handle cannot be dereferenced");
                return link_.currentLink();
            }
        }

        // this one is instead of c++ -> and () operators overload
        public static implicit operator T(Handle<T> ImpliedObject) => ImpliedObject.link;

        //! dereferencing
        public T currentLink() => link;

        //! checks if the contained shared pointer points to anything
        public bool empty() => link_.empty();

        // dereferencing of the observable to the Link
        public void registerWith(Callback handler)
        {
            link_.registerWith(handler);
        }

        public void unregisterWith(Callback handler)
        {
            link_.unregisterWith(handler);
        }

        #region operator overload

        public static bool operator ==(Handle<T> here, Handle<T> there)
        {
            if (ReferenceEquals(here, there))
            {
                return true;
            }

            if ((object)here == null || (object)there == null)
            {
                return false;
            }

            return here.Equals(there);
        }

        public static bool operator !=(Handle<T> here, Handle<T> there) => !(here == there);

        public override bool Equals(object o) => link_ == ((Handle<T>)o).link_;

        public override int GetHashCode() => ToString().GetHashCode();

        #endregion operator overload
    }

    //! Relinkable handle to an observable
    /*! An instance of this class can be relinked so that it points to another observable. The change will be propagated to all
        handles that were created as copies of such instance. */
}
