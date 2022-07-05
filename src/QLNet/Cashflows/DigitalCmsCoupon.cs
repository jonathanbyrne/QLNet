/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet.Cashflows
{
    //! Cms-rate coupon with digital digital call/put option
    [PublicAPI]
    public class DigitalCmsCoupon : DigitalCoupon
    {
        // need by CashFlowVectors
        public DigitalCmsCoupon()
        {
        }

        public DigitalCmsCoupon(CmsCoupon underlying,
            double? callStrike = null,
            Position.Type callPosition = Position.Type.Long,
            bool isCallATMIncluded = false,
            double? callDigitalPayoff = null,
            double? putStrike = null,
            Position.Type putPosition = Position.Type.Long,
            bool isPutATMIncluded = false,
            double? putDigitalPayoff = null,
            DigitalReplication replication = null)
            : base(underlying, callStrike, callPosition, isCallATMIncluded, callDigitalPayoff, putStrike, putPosition, isPutATMIncluded, putDigitalPayoff, replication)
        {
        }

        // Factory - for Leg generators
        public virtual CashFlow factory(CmsCoupon underlying, double? callStrike, Position.Type callPosition, bool isCallATMIncluded, double? callDigitalPayoff, double? putStrike, Position.Type putPosition, bool isPutATMIncluded, double? putDigitalPayoff, DigitalReplication replication) => new DigitalCmsCoupon(underlying, callStrike, callPosition, isCallATMIncluded, callDigitalPayoff, putStrike, putPosition, isPutATMIncluded, putDigitalPayoff, replication);
    }

    //! helper class building a sequence of digital ibor-rate coupons
}
