/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available online at <http://qlnet.sourceforge.net/License.html>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    [PublicAPI]
    public class FdmQuantoHelper : IObservable
    {
        protected double equityFxCorrelation_, exchRateATMlevel_;
        protected BlackVolTermStructure fxVolTS_;
        protected YieldTermStructure rTS_, fTS_;

        public FdmQuantoHelper(
            YieldTermStructure rTS,
            YieldTermStructure fTS,
            BlackVolTermStructure fxVolTS,
            double equityFxCorrelation,
            double exchRateATMlevel)
        {
            rTS_ = rTS;
            fTS_ = fTS;
            fxVolTS_ = fxVolTS;
            equityFxCorrelation_ = equityFxCorrelation;
            exchRateATMlevel_ = exchRateATMlevel;
        }

        public double equityFxCorrelation() => equityFxCorrelation_;

        public double exchRateATMlevel() => exchRateATMlevel_;

        public YieldTermStructure foreignTermStructure() => fTS_;

        public BlackVolTermStructure fxVolatilityTermStructure() => fxVolTS_;

        public double quantoAdjustment(double equityVol, double t1, double t2)
        {
            var rDomestic = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
            var rForeign = fTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
            var fxVol = fxVolTS_.blackForwardVol(t1, t2, exchRateATMlevel_);

            return rDomestic - rForeign + equityVol * fxVol * equityFxCorrelation_;
        }

        public Vector quantoAdjustment(Vector equityVol, double t1, double t2)
        {
            var rDomestic = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
            var rForeign = fTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
            var fxVol = fxVolTS_.blackForwardVol(t1, t2, exchRateATMlevel_);

            var retVal = new Vector(equityVol.size());
            for (var i = 0; i < retVal.size(); ++i)
            {
                retVal[i] = rDomestic - rForeign + equityVol[i] * fxVol * equityFxCorrelation_;
            }

            return retVal;
        }

        public YieldTermStructure riskFreeTermStructure() => rTS_;

        #region Observer & Observable

        // observable interface
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
}
