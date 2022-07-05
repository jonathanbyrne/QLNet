/*
 Copyright (C) 2008 Andrea Maggiulli
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
using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Bulgarian lev
    ///     The ISO three-letter code is BGL; the numeric code is 100.
    ///     It is divided in 100 stotinki.
    /// </summary>
    [PublicAPI]
    public class BGLCurrency : Currency
    {
        public BGLCurrency() : base("Bulgarian lev", "BGL", 100, "lv", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }

    //! Russian ruble
    /*! The ISO three-letter code is RUB; the numeric code is 643.
        It is divided in 100 kopeyki.

        \ingroup currencies
    */

    // currencies obsoleted by Euro

    //! Ukrainian hryvnia
    /*! The ISO three-letter code is UAH; the numeric code is 980.
       It is divided in 100 kopiykas.

       \ingroup currencies
    */
}
