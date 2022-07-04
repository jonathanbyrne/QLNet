/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

using QLNet.Termstructures;
using QLNet.Time;
using System.Collections.Generic;
using System.Reflection;

namespace QLNet
{

    //! generic pricer for floating-rate coupons

    //! base pricer for capped/floored Ibor coupons

    /*! Black-formula pricer for capped/floored Ibor coupons
       References for timing adjustments
       Black76             Hull, Options, Futures and other
                           derivatives, 4th ed., page 550
       BivariateLognormal  http://ssrn.com/abstract=2170721
       The bivariate lognormal adjustment implementation is
       still considered experimental */

    //! base pricer for vanilla CMS coupons

    /*! (CMS) coupon pricer that has a mean reversion parameter which can be
      used to calibrate to cms market quotes */

    //===========================================================================//
   //                         CouponSelectorToSetPricer                         //
   //===========================================================================//

   partial class Utils
   {
      public static void setCouponPricer(List<CashFlow> leg, FloatingRateCouponPricer pricer)
      {
         var setter = new PricerSetter(pricer);
         foreach (var cf in leg)
         {
            cf.accept(setter);
         }
      }

      public static void setCouponPricers(List<CashFlow> leg, List<FloatingRateCouponPricer> pricers)
      {
         var nCashFlows = leg.Count;
         Utils.QL_REQUIRE(nCashFlows > 0, () => "no cashflows");

         var nPricers = pricers.Count;
         Utils.QL_REQUIRE(nCashFlows >= nPricers, () =>
                          "mismatch between leg size (" + nCashFlows +
                          ") and number of pricers (" + nPricers + ")");

         for (var i = 0; i < nCashFlows; ++i)
         {
            var setter = new PricerSetter(i < nPricers ? pricers[i] : pricers[nPricers - 1]);
            leg[i].accept(setter);
         }
      }
   }
}
