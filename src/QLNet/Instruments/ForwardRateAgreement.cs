﻿/*
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

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! %Forward rate agreement (FRA) class
    /*! 1. Unlike the forward contract conventions on carryable
           financial assets (stocks, bonds, commodities), the
           valueDate for a FRA is taken to be the day when the forward
           loan or deposit begins and when full settlement takes place
           (based on the NPV of the contract on that date).
           maturityDate is the date when the forward loan or deposit
           ends. In fact, the FRA settles and expires on the
           valueDate, not on the (later) maturityDate. It follows that
           (maturityDate - valueDate) is the tenor/term of the
           underlying loan or deposit

        2. Choose position ExerciseType = Long for an "FRA purchase" (future
           long loan, short deposit [borrower])

        3. Choose position ExerciseType = Short for an "FRA sale" (future short
           loan, long deposit [lender])

        4. If strike is given in the constructor, can calculate the NPV
           of the contract via NPV().

        5. If forward rate is desired/unknown, it can be obtained via
           forwardRate(). In this case, the strike variable in the
           constructor is irrelevant and will be ignored.

        <b>Example: </b>
        \link FRA.cs
        valuation of a forward-rate agreement
        \endlink

        \todo Add preconditions and tests

        \todo Should put an instance of ForwardRateAgreement in the
              FraRateHelper to ensure consistency with the piecewise
              yield curve.

        \todo Differentiate between BBA (British)/AFB (French)
              [assumed here] and ABA (Australian) banker conventions
              in the calculations.

        \warning This class still needs to be rigorously tested

        \ingroup instruments
    */

    [PublicAPI]
    public class ForwardRateAgreement : Forward
    {
        //! aka FRA rate (the market forward rate)
        protected InterestRate forwardRate_;
        protected Position.Type fraType_;
        protected IborIndex index_;
        protected double notionalAmount_;
        //! aka FRA fixing rate, contract rate
        protected InterestRate strikeForwardRate_;

        public ForwardRateAgreement(Date valueDate, Date maturityDate, Position.Type type, double strikeForwardRate,
            double notionalAmount, IborIndex index, Handle<YieldTermStructure> discountCurve)
            : base(
                index.dayCounter(), index.fixingCalendar(), index.businessDayConvention(), index.fixingDays(), new Payoff(),
                valueDate, maturityDate, discountCurve)
        {
            fraType_ = type;
            notionalAmount_ = notionalAmount;
            index_ = index;

            QLNet.Utils.QL_REQUIRE(notionalAmount > 0.0, () => "notional Amount must be positive");

            // do I adjust this ?
            var fixingDate = calendar_.advance(valueDate_, -settlementDays_, TimeUnit.Days);
            forwardRate_ = new InterestRate(index.fixing(fixingDate), index.dayCounter(), Compounding.Simple, Frequency.Once);
            strikeForwardRate_ = new InterestRate(strikeForwardRate, index.dayCounter(), Compounding.Simple, Frequency.Once);
            var strike = notionalAmount_ * strikeForwardRate_.compoundFactor(valueDate_, maturityDate_);
            payoff_ = new ForwardTypePayoff(fraType_, strike);
            // incomeDiscountCurve_ is irrelevant to an FRA
            incomeDiscountCurve_ = discountCurve_;
            // income is irrelevant to FRA - set it to zero
            underlyingIncome_ = 0.0;

            index_.registerWith(update);
        }

        //! Returns the relevant forward rate associated with the FRA term
        public InterestRate forwardRate()
        {
            calculate();
            return forwardRate_;
        }

        /*! A FRA expires/settles on the valueDate */

        public override bool isExpired() => new simple_event(valueDate_).hasOccurred(settlementDate());

        // Calculations
        public override Date settlementDate() => calendar_.advance(Settings.evaluationDate(), settlementDays_, TimeUnit.Days);

        /*!  Income is zero for a FRA */

        public override double spotIncome(Handle<YieldTermStructure> t) => 0.0;

        //! Spot value (NPV) of the underlying loan
        /*! This has always a positive value (asset), even if short the FRA */

        public override double spotValue()
        {
            calculate();
            var result = notionalAmount_ *
                         forwardRate().compoundFactor(valueDate_, maturityDate_) *
                         discountCurve_.link.discount(maturityDate_);
            return result;
        }

        protected override void performCalculations()
        {
            var fixingDate = calendar_.advance(valueDate_, -settlementDays_, TimeUnit.Days);
            forwardRate_ = new InterestRate(index_.fixing(fixingDate), index_.dayCounter(),
                Compounding.Simple, Frequency.Once);
            underlyingSpotValue_ = spotValue();
            underlyingIncome_ = 0.0;
            base.performCalculations();
        }
    }
}
