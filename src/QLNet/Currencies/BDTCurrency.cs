/*
 Copyright (C) 2008 Andrea Maggiulli

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

namespace QLNet.Currencies
{
    //! Bangladesh taka
    /*! The ISO three-letter code is BDT; the numeric code is 50.
        It is divided in 100 paisa.
           \ingroup currencies
    */
    [JetBrains.Annotations.PublicAPI] public class BDTCurrency : Currency
    {
        public BDTCurrency() : base("Bangladesh taka", "BDT", 50, "Bt", "", 100, new Rounding(), "%3% %1$.2f") { }
    }

    //! Chinese yuan
    /*! The ISO three-letter code is CNY; the numeric code is 156.
       It is divided in 100 fen.

       \ingroup currencies
    */

    //! Hong Kong dollar
    /*! The ISO three-letter code is HKD; the numeric code is 344.
       It is divided in 100 cents.

       \ingroup currencies
    */

    //! Indonesian Rupiah
    /*! The ISO three-letter code is IDR; the numeric code is 360.
        It is divided in 100 sen.

        \ingroup currencies
    */

    //! Israeli shekel
    /*! The ISO three-letter code is ILS; the numeric code is 376.
       It is divided in 100 agorot.

       \ingroup currencies
    */

    //! Indian rupee
    /*! The ISO three-letter code is INR; the numeric code is 356.
       It is divided in 100 paise.

       \ingroup currencies
    */

    //! Iraqi dinar
    /*! The ISO three-letter code is IQD; the numeric code is 368.
       It is divided in 1000 fils.

       \ingroup currencies
    */

    //! Iranian rial
    /*! The ISO three-letter code is IRR; the numeric code is 364.
       It has no subdivisions.

       \ingroup currencies
    */

    //! South-Korean won
    /*! The ISO three-letter code is KRW; the numeric code is 410.
       It is divided in 100 chon.

       \ingroup currencies
    */

    //! Kuwaiti dinar
    /*! The ISO three-letter code is KWD; the numeric code is 414.
       It is divided in 1000 fils.

       \ingroup currencies
    */

    //! Malaysian Ringgit
    /*! The ISO three-letter code is MYR; the numeric code is 458.
        It is divided in 100 sen.

        \ingroup currencies
    */

    //! Nepal rupee
    /*! The ISO three-letter code is NPR; the numeric code is 524.
       It is divided in 100 paise.

       \ingroup currencies
    */

    //! Pakistani rupee
    /*! The ISO three-letter code is PKR; the numeric code is 586.
       It is divided in 100 paisa.

       \ingroup currencies
    */

    //! Saudi riyal
    /*! The ISO three-letter code is SAR; the numeric code is 682.
       It is divided in 100 halalat.

       \ingroup currencies
    */

    //! %Singapore dollar
    /*! The ISO three-letter code is SGD; the numeric code is 702.
       It is divided in 100 cents.

       \ingroup currencies
    */

    //! Thai baht
    /*! The ISO three-letter code is THB; the numeric code is 764.
       It is divided in 100 stang.

       \ingroup currencies
    */

    //! %Taiwan dollar
    /*! The ISO three-letter code is TWD; the numeric code is 901.
       It is divided in 100 cents.

       \ingroup currencies
    */

    //! Vietnamese Dong
    /*! The ISO three-letter code is VND; the numeric code is 704.
        It was divided in 100 xu.

        \ingroup currencies
    */
}
