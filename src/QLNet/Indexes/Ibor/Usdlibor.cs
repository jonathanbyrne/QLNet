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
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    //! %USD %LIBOR rate
    /*! US Dollar LIBOR fixed by ICE.

        See <https://www.theice.com/marketdata/reports/170>.
    */
    [PublicAPI]
    public class USDLibor : Libor
    {
        public USDLibor(Period tenor) : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public USDLibor(Period tenor, Handle<YieldTermStructure> h)
            : base("USDLibor", tenor, 2, new USDCurrency(), new UnitedStates(UnitedStates.Market.Settlement), new Actual360(), h)
        {
        }
    }

    //! base class for the one day deposit ICE %USD %LIBOR indexes

    //! Overnight %USD %Libor index
}
