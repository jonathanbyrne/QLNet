/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Time;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
    //! Base exercise class
    [JetBrains.Annotations.PublicAPI] public class Exercise
   {
      public enum Type
      {
         American,
         Bermudan,
         European
      }

      protected Type type_;

      public Type ExerciseType() => type_;

      protected List<Date> dates_;

      public List<Date> dates() => dates_;

      // constructor
      public Exercise(Type type)
      {
         type_ = type;
      }

      // inspectors
      public Date date(int index) => dates_[index];

      public Date lastDate() => dates_.Last();
   }

   //! Early-exercise base class
   /*! The payoff can be at exercise (the default) or at expiry */

   //! American exercise
   /*! An American option can be exercised at any time between two
       predefined dates; the first date might be omitted, in which
       case the option can be exercised at any time before the expiry.

       \todo check that everywhere the American condition is applied
             from earliestDate and not earlier
   */

   //! Bermudan exercise
   /*! A Bermudan option can only be exercised at a set of fixed dates. */

   //! European exercise
   /*! A European option can only be exercised at one (expiry) date. */
}
