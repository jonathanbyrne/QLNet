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
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet
{
    /// <summary>
    ///     %Euribor index
    ///     Euribor rate fixed by the ECB.
    ///     This is the rate fixed by the ECB. Use EurLibor if you're interested in the London fixing by BBA.
    /// </summary>
    [PublicAPI]
    public class Euribor : IborIndex
    {
        public Euribor(Period tenor) : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public Euribor(Period tenor, Handle<YieldTermStructure> h) :
            base("Euribor", tenor, 2, // settlementDays
                new EURCurrency(), new TARGET(),
                Utils.euriborConvention(tenor), Utils.euriborEOM(tenor),
                new Actual360(), h)
        {
            Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days, () =>
                "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
        }
    }

    //! Actual/365 %Euribor index
    /*! Euribor rate adjusted for the mismatch between the actual/360
        convention used for Euribor and the actual/365 convention
        previously used by a few pre-EUR currencies.
    */

    //! 1-week %Euribor index

    //! 2-weeks %Euribor index

    //! 3-weeks %Euribor index

    //! 1-month %Euribor index

    //! 2-months %Euribor index

    // 3-months %Euribor index

    // 4-months %Euribor index

    // 5-months %Euribor index

    // 6-months %Euribor index

    // 1-year %Euribor index
}
