/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    //! A transformation of an existing inflation swap rate.
    /*! This is an abstract class and contains the functions
        correctXXXRate which returns rates with the seasonality
        correction.  Currently only the price multiplicative version
        is implemented, but this covers stationary (1-year) and
        non-stationary (multi-year) seasonality depending on how many
        years of factors are given.  Seasonality is piecewise
        constant, hence it will work with un-interpolated inflation
        indices.

        A seasonality assumption can be used to fill in inflation swap
        curves between maturities that are usually given in integer
        numbers of years, e.g. 8,9,10,15,20, etc.  Historical
        seasonality may be observed in reported CPI values,
        alternatively it may be affected by known future events, e.g.
        announced changes in VAT rates.  Thus seasonality may be
        stationary or non-stationary.

        If seasonality is additive then both swap rates will show
        affects.  Additive seasonality is not implemented.
    */
    [PublicAPI]
    public class Seasonality
    {
        public virtual double correctYoYRate(Date d, double r, InflationTermStructure iTS) => 0;

        // Seasonality interface
        public virtual double correctZeroRate(Date d, double r, InflationTermStructure iTS) => 0;

        /*! It is possible for multi-year seasonalities to be
            inconsistent with the inflation term structure they are
            given to.  This method enables testing - but programmers
            are not required to implement it.  E.g. for price
            seasonality the corrections at whole years after the
            inflation curve base date should be the same or else there
            can be an inconsistency with quoted instruments.
            Alternatively, the seasonality can be set _before_ the
            inflation curve is bootstrapped.
        */
        public virtual bool isConsistent(InflationTermStructure iTS) => true;
    }

    //! Multiplicative seasonality in the price index (CPI/RPI/HICP/etc).
    /*! Stationary multiplicative seasonality in CPI/RPI/HICP (i.e. in
      price) implies that zero inflation swap rates are affected,
      but that year-on-year inflation swap rates show no effect.  Of
      course, if the seasonality in CPI/RPI/HICP is non-stationary
      then both swap rates will be affected.

      Factors must be in multiples of the minimum required for one
      year, e.g. 12 for monthly, and these factors are reused for as
      long as is required, i.e. they wrap around.  So, for example,
      if 24 factors are given this repeats every two years.  True
      stationary seasonality can be obtained by giving the same
      number of factors as the frequency dictates e.g. 12 for
      monthly seasonality.

      \warning Multi-year seasonality (i.e. non-stationary) is
               fragile: the user <b>must</b> ensure that corrections
               at whole years before and after the inflation term
               structure base date are the same.  Otherwise there
               can be an inconsistency with quoted rates.  This is
               enforced if the frequency is lower than daily.  This
               is not enforced for daily seasonality because this
               will always be inconsistent due to weekends,
               holidays, leap years, etc.  If you use multi-year
               daily seasonality it is up to you to check.

      \note Factors are normalized relative to their appropriate
            reference dates.  For zero inflation this is the
            inflation curve true base date: since you have a fixing
            for that date the seasonality factor must be one.  For
            YoY inflation the reference is always one year earlier.

      Seasonality is treated as piecewise constant, hence it works
      correctly with uninterpolated indices if the seasonality
      correction factor frequency is the same as the index frequency
      (or less).
    */
}
