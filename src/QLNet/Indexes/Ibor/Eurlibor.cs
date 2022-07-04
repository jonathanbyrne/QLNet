/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using System;
using QLNet.Currencies;
using QLNet.Time.DayCounters;

namespace QLNet
{
    public static partial class Utils
   {
      public static BusinessDayConvention eurliborConvention(Period p)
      {
         switch (p.units())
         {
            case TimeUnit.Days:
            case TimeUnit.Weeks:
               return BusinessDayConvention.Following;
            case TimeUnit.Months:
            case TimeUnit.Years:
               return BusinessDayConvention.ModifiedFollowing;
            default:
               throw new ArgumentException("Unknown TimeUnit: " + p.units());
         }
      }

      public static bool eurliborEOM(Period p)
      {
         switch (p.units())
         {
            case TimeUnit.Days:
            case TimeUnit.Weeks:
               return false;
            case TimeUnit.Months:
            case TimeUnit.Years:
               return true;
            default:
               throw new ArgumentException("Unknown TimeUnit: " + p.units());
         }
      }
   }

   //! base class for all ICE %EUR %LIBOR indexes but the O/N
   /*! Euro LIBOR fixed by ICE.

       See <https://www.theice.com/marketdata/reports/170>.

       \warning This is the rate fixed in London by BBA. Use Euribor if
                you're interested in the fixing by the ECB.
   */
   [JetBrains.Annotations.PublicAPI] public class EURLibor : IborIndex
   {
      private Calendar target_;

      // http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1412 :
      // JoinBusinessDays is the fixing calendar for
      // all indexes but o/n

      public EURLibor(Period tenor)
         : base("EURLibor", tenor, 2, new EURCurrency(), new JointCalendar(new UnitedKingdom(UnitedKingdom.Market.Exchange), new TARGET(),
                                                                           JointCalendar.JointCalendarRule.JoinHolidays),
                Utils.eurliborConvention(tenor), Utils.eurliborEOM(tenor), new Actual360(),
                new Handle<YieldTermStructure>())
      {
         target_ = new TARGET();
         Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days, () =>
                          "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
      }

      public EURLibor(Period tenor, Handle<YieldTermStructure> h)
         : base("EURLibor", tenor, 2, new EURCurrency(), new JointCalendar(new UnitedKingdom(UnitedKingdom.Market.Exchange), new TARGET(),
                                                                           JointCalendar.JointCalendarRule.JoinHolidays),
                Utils.eurliborConvention(tenor), Utils.eurliborEOM(tenor), new Actual360(), h)
      {
         target_ = new TARGET();
         Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days, () =>
                          "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
      }

      /* Date calculations

            See <https://www.theice.com/marketdata/reports/170>.
      */
      public override Date valueDate(Date fixingDate)
      {

         Utils.QL_REQUIRE(isValidFixingDate(fixingDate), () => "Fixing date " + fixingDate + " is not valid");

         // http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1412 :
         // In the case of EUR the Value Date shall be two TARGET
         // business days after the Fixing Date.
         return target_.advance(fixingDate, fixingDays_, TimeUnit.Days);
      }
      public override Date maturityDate(Date valueDate) =>
          // http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1412 :
          // In the case of EUR only, maturity dates will be based on days in
          // which the Target system is open.
          target_.advance(valueDate, tenor_, convention_, endOfMonth());
   }

   //! base class for the one day deposit ICE %EUR %LIBOR indexes
   /*! Euro O/N LIBOR fixed by ICE. It can be also used for T/N and S/N
       indexes, even if such indexes do not have ICE fixing.

       See <https://www.theice.com/marketdata/reports/170>.

       \warning This is the rate fixed in London by ICE. Use Eonia if
                you're interested in the fixing by the ECB.
   */

   //! Overnight %EUR %Libor index

   //! 1-week %EUR %Libor index

   //! 2-weeks %EUR %Libor index

   //! 1-month %EUR %Libor index

   //! 2-months %EUR %Libor index

   //! 3-months %EUR %Libor index

   //! 4-months %EUR %Libor index

   //! 5-months %EUR %Libor index

   //! 6-months %EUR %Libor index

   //! 7-months %EUR %Libor index

   //! 8-months %EUR %Libor index

   //! 9-months %EUR %Libor index

   //! 10-months %EUR %Libor index

   //! 11-months %EUR %Libor index

   //! 1-year %EUR %Libor index
}
