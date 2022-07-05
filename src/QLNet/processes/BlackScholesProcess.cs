/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
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

using JetBrains.Annotations;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Processes
{
    //! Generalized Black-Scholes stochastic process
    /*! This class describes the stochastic process \f$ S \f$ governed by
        \f[
            d\ln S(t) = (r(t) - q(t) - \frac{\sigma(t, S)^2}{2}) dt
                     + \sigma dW_t.
        \f]

        \warning while the interface is expressed in terms of \f$ S \f$,
                 the internal calculations work on \f$ ln S \f$.

        \ingroup processes
    */

    //! Black-Scholes (1973) stochastic process
    /*! This class describes the stochastic process S for a stock given by
        \f[
            dS(t, S) = (r(t) - \frac{\sigma(t, S)^2}{2}) dt + \sigma dW_t.
        \f]

        \ingroup processes
    */

    [PublicAPI]
    public class BlackScholesProcess : GeneralizedBlackScholesProcess
    {
        public BlackScholesProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS)
            : this(x0, riskFreeTS, blackVolTS, new EulerDiscretization())
        {
        }

        public BlackScholesProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IDiscretization1D d)
            : base(x0,
                // no dividend yield
                new Handle<YieldTermStructure>(new FlatForward(0, new NullCalendar(), 0.0, new Actual365Fixed())),
                riskFreeTS, blackVolTS, d)
        {
        }
    }

    //! Merton (1973) extension to the Black-Scholes stochastic process
    /*! This class describes the stochastic process for a stock or
        stock index paying a continuous dividend yield given by
        \f[
            dS(t, S) = (r(t) - q(t) - \frac{\sigma(t, S)^2}{2}) dt
                     + \sigma dW_t.
        \f]

        \ingroup processes
    */

    //! Black (1976) stochastic process
    /*! This class describes the stochastic process for a forward or
        futures contract given by
        \f[
            dS(t, S) = \frac{\sigma(t, S)^2}{2} dt + \sigma dW_t.
        \f]

        \ingroup processes
    */

    //! Garman-Kohlhagen (1983) stochastic process
    /*! This class describes the stochastic process for an exchange
        rate given by
        \f[
            dS(t, S) = (r(t) - r_f(t) - \frac{\sigma(t, S)^2}{2}) dt
                     + \sigma dW_t.
        \f]

        \ingroup processes
    */
}
