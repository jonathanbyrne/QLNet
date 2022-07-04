/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
    // interface for all value methods

    public struct Const
   {
      public const double QL_EPSILON = 2.2204460492503131e-016;

      public const double M_SQRT2    = 1.41421356237309504880;
      public const double M_SQRT_2   = 0.7071067811865475244008443621048490392848359376887;
      public const double M_SQRTPI   = 1.77245385090551602792981;
      public const double M_1_SQRTPI = 0.564189583547756286948;

      public const double M_LN2 = 0.693147180559945309417;
      public const double M_PI = 3.141592653589793238462643383280;
      public const double M_PI_2 = 1.57079632679489661923;
      public const double M_2_PI = 0.636619772367581343076;

      public static double BASIS_POINT = 1.0e-4;
   }

    //! Interest rate coumpounding rule

    //! Units used to describe time periods

    // These conventions specify the rule used to generate dates in a Schedule.
}
