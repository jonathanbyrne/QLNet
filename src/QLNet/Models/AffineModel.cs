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
using QLNet.Math;
using QLNet.Patterns;
using System;

namespace QLNet.Models
{
    //! Affine model class
    /*! Base class for analytically tractable models.

        \ingroup shortrate
    */
    public abstract class AffineModel : IObservable
    {
        //! Implied discount curve
        public abstract double discount(double t);
        public abstract double discountBond(double now, double maturity, Vector factors);
        public abstract double discountBondOption(Option.Type type, double strike, double maturity, double bondMaturity);

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
    }

    //Affince Model Interface used for multihritage in
    //liborforwardmodel.cs & analyticcapfloorengine.cs

    //TermStructureConsistentModel used in analyticcapfloorengine.cs

    //ITermStructureConsistentModel used ins shortratemodel blackkarasinski.cs/hullwhite.cs

    //! Calibrated model class

    //! Abstract short-rate model class
    /*! \ingroup shortrate */
}
