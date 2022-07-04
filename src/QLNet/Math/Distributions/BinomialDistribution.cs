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

using QLNet.Extensions;
using System;

namespace QLNet
{
    //! Binomial probability distribution function
    /*! formula here ...
        Given an integer k it returns its probability in a Binomial
        distribution with parameters p and n.
    */

    [JetBrains.Annotations.PublicAPI] public class BinomialDistribution
   {
      private int n_;
      private double logP_, logOneMinusP_;

      public BinomialDistribution(double p, int n)
      {
         n_ = n;

         if (p.IsEqual(0.0))
         {
            logOneMinusP_ = 0.0;
         }
         else if (p.IsEqual(1.0))
         {
            logP_ = 0.0;
         }
         else
         {
            Utils.QL_REQUIRE(p > 0, () => "negative p not allowed");
            Utils.QL_REQUIRE(p < 1.0, () => "p>1.0 not allowed");

            logP_ = System.Math.Log(p);
            logOneMinusP_ = System.Math.Log(1.0 - p);
         }
      }

      // function
      public double value(int k)
      {
         if (k > n_) return 0.0;

         // p==1.0
         if (logP_.IsEqual(0.0))
            return (k == n_ ? 1.0 : 0.0);
         // p==0.0
         if (logOneMinusP_.IsEqual(0.0))
            return (k == 0 ? 1.0 : 0.0);
         return System.Math.Exp(Utils.binomialCoefficientLn(n_, k) + k * logP_ + (n_ - k) * logOneMinusP_);
      }
   }

   //! Cumulative binomial distribution function
   /*! Given an integer k it provides the cumulative probability
       of observing kk<=k:
       formula here ...
   */
}
