/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Cashflows
{
    public abstract class PrincipalLegBase
    {
        protected Schedule schedule_;
        protected List<double> notionals_;
        protected BusinessDayConvention paymentAdjustment_;
        protected DayCounter dayCounter_;
        protected int sign_;
        public static implicit operator List<CashFlow>(PrincipalLegBase o) => o.value();

        public abstract List<CashFlow> value();


        // initializers
        public PrincipalLegBase withNotionals(double notional)
        {
            notionals_ = new List<double>() { notional };
            return this;
        }
        public PrincipalLegBase withNotionals(List<double> notionals)
        {
            notionals_ = notionals;
            return this;
        }
        public PrincipalLegBase withPaymentAdjustment(BusinessDayConvention convention)
        {
            paymentAdjustment_ = convention;
            return this;
        }
        public PrincipalLegBase withPaymentDayCounter(DayCounter dayCounter)
        {
            dayCounter_ = dayCounter;
            return this;
        }

        public PrincipalLegBase withSign(int sign)
        {
            sign_ = sign;
            return this;
        }

    }

    //! helper class building a Bullet Principal leg

    //! helper class building a Bullet Principal leg
}
