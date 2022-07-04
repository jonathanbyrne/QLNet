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

using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class MakeFixedLoan
    {
        private double nominal_;
        private Calendar calendar_;
        private Date startDate_, endDate_;
        private Frequency frequency_;
        private BusinessDayConvention convention_;
        private DayCounter dayCounter_;
        private double fixedRate_;
        private Loan.Type type_;
        private Loan.Amortising amortising_;
        private DateGeneration.Rule rule_;
        private bool endOfMonth_;

        public MakeFixedLoan(Date startDate, Date endDate, double fixedRate, Frequency frequency)
        {
            startDate_ = startDate;
            endDate_ = endDate;
            fixedRate_ = fixedRate;
            frequency_ = frequency;

            type_ = Loan.Type.Loan;
            amortising_ = Loan.Amortising.Bullet;
            nominal_ = 1.0;
            calendar_ = new TARGET();
            convention_ = BusinessDayConvention.ModifiedFollowing;
            dayCounter_ = new Actual365Fixed();
            rule_ = DateGeneration.Rule.Forward;
            endOfMonth_ = false;
        }

        public MakeFixedLoan withType(Loan.Type type)
        {
            type_ = type;
            return this;
        }

        public MakeFixedLoan withNominal(double n)
        {
            nominal_ = n;
            return this;
        }

        public MakeFixedLoan withCalendar(Calendar c)
        {
            calendar_ = c;
            return this;
        }

        public MakeFixedLoan withConvention(BusinessDayConvention bdc)
        {
            convention_ = bdc;
            return this;
        }

        public MakeFixedLoan withDayCounter(DayCounter dc)
        {
            dayCounter_ = dc;
            return this;
        }

        public MakeFixedLoan withRule(DateGeneration.Rule r)
        {
            rule_ = r;
            return this;
        }

        public MakeFixedLoan withEndOfMonth(bool flag)
        {
            endOfMonth_ = flag;
            return this;
        }

        public MakeFixedLoan withAmortising(Loan.Amortising Amortising)
        {
            amortising_ = Amortising;
            return this;
        }

        // Loan creator
        public static implicit operator FixedLoan(MakeFixedLoan o) => o.value();

        public FixedLoan value()
        {

            var fixedSchedule = new Schedule(startDate_, endDate_, new Period(frequency_),
                                                  calendar_, convention_, convention_, rule_, endOfMonth_);

            var principalPeriod = amortising_ == Loan.Amortising.Bullet ?
                                     new Period(Frequency.Once) :
                                     new Period(frequency_);

            var principalSchedule = new Schedule(startDate_, endDate_, principalPeriod,
                                                      calendar_, convention_, convention_, rule_, endOfMonth_);

            var fl = new FixedLoan(type_, nominal_, fixedSchedule, fixedRate_, dayCounter_,
                                         principalSchedule, convention_);
            return fl;

        }

    }
}
