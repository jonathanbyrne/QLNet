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

using System;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet
{
    //! discretization of a stochastic process over a given time interval

    //! discretization of a 1D stochastic process over a given time interval

    //! multi-dimensional stochastic process class.
    /*! This class describes a stochastic process governed by
        \f[
        d\mathrm{x}_t = \mu(t, x_t)\mathrm{d}t
                      + \sigma(t, \mathrm{x}_t) \cdot d\mathrm{W}_t.
        \f]
    */
    public abstract class StochasticProcess : IObservable, IObserver
    {
        protected IDiscretization discretization_;

        protected StochasticProcess()
        {
        }

        protected StochasticProcess(IDiscretization disc)
        {
            discretization_ = disc;
        }

        /*! \brief returns the diffusion part of the equation, i.e.
                   \f$ \sigma(t, \mathrm{x}_t) \f$ */
        public abstract Matrix diffusion(double t, Vector x);

        /*! \brief returns the drift part of the equation, i.e.,
                   \f$ \mu(t, \mathrm{x}_t) \f$ */
        public abstract Vector drift(double t, Vector x);

        //! returns the initial values of the state variables
        public abstract Vector initialValues();

        // Stochastic process interface
        //! returns the number of dimensions of the stochastic process
        public abstract int size();

        // applies a change to the asset value.
        public virtual Vector apply(Vector x0, Vector dx) => x0 + dx;

        /*! returns the covariance. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Matrix covariance(double t0, Vector x0, double dt) => discretization_.covariance(this, t0, x0, dt);

        // returns the asset value after a time interval
        public virtual Vector evolve(double t0, Vector x0, double dt, Vector dw) => apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);

        /*! This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Vector expectation(double t0, Vector x0, double dt) => apply(x0, discretization_.drift(this, t0, x0, dt));

        //! returns the number of independent factors of the process
        public virtual int factors() => size();

        /*! returns the standard deviation. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Matrix stdDeviation(double t0, Vector x0, double dt) => discretization_.diffusion(this, t0, x0, dt);

        // utilities
        /*! returns the time value corresponding to the given date
            in the reference system of the stochastic process.
  
            \note As a number of processes might not need this
                  functionality, a default implementation is given
                  which raises an exception.
        */
        public virtual double time(Date d) => throw new NotSupportedException("date/time conversion not supported");

        #region Observer & Observable

        // Subjects, i.e. observables, should define interface internally like follows.
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

        public virtual void update()
        {
            notifyObservers();
        }

        #endregion
    }

    //! 1-dimensional stochastic process
}
