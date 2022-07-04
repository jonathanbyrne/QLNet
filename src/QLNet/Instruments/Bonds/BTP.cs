/*
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
using QLNet.Time.DayCounters;
using System.Collections.Generic;
using QLNet.Math;
using QLNet.Time.Calendars;

namespace QLNet.Instruments.Bonds
{
    /*! Italian CCTEU (Certificato di credito del tesoro)
         Euribor6M indexed floating rate bond

         \ingroup instruments

    */

    //! Italian BTP (Buono Poliennali del Tesoro) fixed rate bond
    /*! \ingroup instruments

    */
    [JetBrains.Annotations.PublicAPI] public class BTP : FixedRateBond
    {
        public BTP(Date maturityDate, double fixedRate, Date startDate = null, Date issueDate = null)
           : base(2, 100.0, new Schedule(startDate,
                                         maturityDate, new Period(6, TimeUnit.Months),
                                         new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                         DateGeneration.Rule.Backward, true),
                  new List<double> { fixedRate },
        new ActualActual(ActualActual.Convention.ISMA),
        BusinessDayConvention.ModifiedFollowing, 100.0, issueDate, new TARGET())
        { }

        /*! constructor needed for legacy non-par redemption BTPs.
            As of today the only remaining one is IT123456789012
            that will redeem 99.999 on xx-may-2037 */
        public BTP(Date maturityDate, double fixedRate, double redemption, Date startDate = null, Date issueDate = null)
           : base(2, 100.0, new Schedule(startDate,
                                         maturityDate, new Period(6, TimeUnit.Months),
                                         new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                         DateGeneration.Rule.Backward, true),
                  new List<double> { fixedRate },
        new ActualActual(ActualActual.Convention.ISMA),
        BusinessDayConvention.ModifiedFollowing, redemption, issueDate, new TARGET())
        { }
        #region Bond interface

        //! accrued amount at a given date
        /*! The default bond settlement is used if no date is given. */
        public override double accruedAmount(Date d = null)
        {
            var result = base.accruedAmount(d);
            return new ClosestRounding(5).Round(result);
        }

        #endregion

        //! BTP yield given a (clean) price and settlement date
        /*! The default BTP conventions are used: Actual/Actual (ISMA),
            Compounded, Annual.
            The default bond settlement is used if no date is given. */
        public double yield(double cleanPrice, Date settlementDate = null, double accuracy = 1.0e-8, int maxEvaluations = 100) =>
            yield(cleanPrice, new ActualActual(ActualActual.Convention.ISMA),
                Compounding.Compounded, Frequency.Annual, settlementDate, accuracy, maxEvaluations);
    }

    //! RendistatoCalculator equivalent swap lenth Quote adapter

    //! RendistatoCalculator equivalent swap spread Quote adapter
}
