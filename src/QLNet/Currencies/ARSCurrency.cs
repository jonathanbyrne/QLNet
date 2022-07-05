/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
    //! Argentinian peso
    //    ! The ISO three-letter code is ARS; the numeric code is 32.
    //        It is divided in 100 centavos.
    //
    //        \ingroup currencies
    //
    [PublicAPI]
    public class ARSCurrency : Currency
    {
        public ARSCurrency() : base("Argentinian peso", "ARS", 32, "", "", 100, new Rounding(), "%2% %1$.2f")
        {
        }
    }

    //! Brazilian real
    //    ! The ISO three-letter code is BRL; the numeric code is 986.
    //        It is divided in 100 centavos.
    //
    //        \ingroup currencies
    //

    //! Canadian dollar
    //    ! The ISO three-letter code is CAD; the numeric code is 124.
    //        It is divided into 100 cents.
    //
    //        \ingroup currencies
    //

    //! Chilean peso
    //    ! The ISO three-letter code is CLP; the numeric code is 152.
    //        It is divided in 100 centavos.
    //
    //        \ingroup currencies
    //

    //! Colombian peso
    //    ! The ISO three-letter code is COP; the numeric code is 170.
    //        It is divided in 100 centavos.
    //
    //        \ingroup currencies
    //

    //! Mexican peso
    //    ! The ISO three-letter code is MXN; the numeric code is 484.
    //        It is divided in 100 centavos.
    //
    //        \ingroup currencies
    //

    //! Peruvian nuevo sol
    //    ! The ISO three-letter code is PEN; the numeric code is 604.
    //        It is divided in 100 centimos.
    //
    //        \ingroup currencies
    //

    //! Peruvian inti
    //    ! The ISO three-letter code was PEI.
    //        It was divided in 100 centimos. A numeric code is not available
    //        as per ISO 3166-1, we assign 998 as a user-defined code.
    //
    //        Obsoleted by the nuevo sol since July 1991.
    //
    //        \ingroup currencies
    //

    //! Peruvian sol
    //    ! The ISO three-letter code was PEH. A numeric code is not available
    //        as per ISO 3166-1, we assign 999 as a user-defined code.
    //        It was divided in 100 centavos.
    //
    //        Obsoleted by the inti since February 1985.
    //
    //        \ingroup currencies
    //

    //! Trinidad & Tobago dollar
    //    ! The ISO three-letter code is TTD; the numeric code is 780.
    //        It is divided in 100 cents.
    //
    //        \ingroup currencies
    //

    //! U.S. dollar
    //    ! The ISO three-letter code is USD; the numeric code is 840.
    //        It is divided in 100 cents.
    //
    //        \ingroup currencies
    //

    //! Venezuelan bolivar
    //    ! The ISO three-letter code is VEB; the numeric code is 862.
    //        It is divided in 100 centimos.
    //
    //        \ingroup currencies
    //
}
