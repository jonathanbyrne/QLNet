/*
 Copyright (C) 2008, 2009 , 2010, 2011 , 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    //! base pricer for capped/floored CPI coupons N.B. vol-dependent parts are a TODO
    /*! \note this pricer can already do swaplets but to get
              volatility-dependent coupons you need to implement the descendents.
    */
    [PublicAPI]
    public class CPICouponPricer : InflationCouponPricer
    {
        //! data
        protected Handle<CPIVolatilitySurface> capletVol_;
        protected CPICoupon coupon_;
        protected double discount_;
        protected double gearing_;
        protected double spread_;
        protected double spreadLegValue_;

        public CPICouponPricer(Handle<CPIVolatilitySurface> capletVol = null)
        {
            if (capletVol == null)
            {
                capletVol = new Handle<CPIVolatilitySurface>();
            }

            capletVol_ = capletVol;

            if (!capletVol_.empty())
            {
                capletVol_.registerWith(update);
            }
        }

        public override double capletPrice(double effectiveCap)
        {
            var capletPrice = optionletPrice(Option.Type.Call, effectiveCap);
            return gearing_ * capletPrice;
        }

        public override double capletRate(double effectiveCap) => capletPrice(effectiveCap) / (coupon_.accrualPeriod() * discount_);

        public virtual Handle<CPIVolatilitySurface> capletVolatility() => capletVol_;

        public override double floorletPrice(double effectiveFloor)
        {
            var floorletPrice = optionletPrice(Option.Type.Put, effectiveFloor);
            return gearing_ * floorletPrice;
        }

        public override double floorletRate(double effectiveFloor) => floorletPrice(effectiveFloor) / (coupon_.accrualPeriod() * discount_);

        public override void initialize(InflationCoupon coupon)
        {
            coupon_ = coupon as CPICoupon;
            gearing_ = coupon_.fixedRate();
            spread_ = coupon_.spread();
            paymentDate_ = coupon_.date();
            rateCurve_ = ((ZeroInflationIndex)coupon.index())
                .zeroInflationTermStructure().link
                .nominalTermStructure();

            // past or future fixing is managed in YoYInflationIndex::fixing()
            // use yield curve from index (which sets discount)

            discount_ = 1.0;
            if (paymentDate_ > rateCurve_.link.referenceDate())
            {
                discount_ = rateCurve_.link.discount(paymentDate_);
            }

            spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;
        }

        public virtual void setCapletVolatility(Handle<CPIVolatilitySurface> capletVol)
        {
            QLNet.Utils.QL_REQUIRE(!capletVol.empty(), () => "empty capletVol handle");
            capletVol_ = capletVol;
            capletVol_.registerWith(update);
        }

        // InflationCouponPricer interface
        public override double swapletPrice()
        {
            var swapletPrice = adjustedFixing() * coupon_.accrualPeriod() * discount_;
            return gearing_ * swapletPrice + spreadLegValue_;
        }

        public override double swapletRate() =>
            // This way we do not require the index to have
            // a yield curve, i.e. we do not get the problem
            // that a discounting-instrument-pricer is used
            // with a different yield curve
            gearing_ * adjustedFixing() + spread_;

        protected virtual double adjustedFixing(double? fixing = null)
        {
            if (fixing == null)
            {
                fixing = coupon_.indexFixing() / coupon_.baseCPI();
            }

            // no adjustment
            return fixing.Value;
        }

        //! can replace this if really required
        protected virtual double optionletPrice(Option.Type optionType, double effStrike)
        {
            var fixingDate = coupon_.fixingDate();
            if (fixingDate <= Settings.evaluationDate())
            {
                // the amount is determined
                double a, b;
                if (optionType == Option.Type.Call)
                {
                    a = coupon_.indexFixing();
                    b = effStrike;
                }
                else
                {
                    a = effStrike;
                    b = coupon_.indexFixing();
                }

                return System.Math.Max(a - b, 0.0) * coupon_.accrualPeriod() * discount_;
            }

            // not yet determined, use Black/DD1/Bachelier/whatever from Impl
            QLNet.Utils.QL_REQUIRE(!capletVolatility().empty(), () => "missing optionlet volatility");
            var stdDev = System.Math.Sqrt(capletVolatility().link.totalVariance(fixingDate, effStrike));
            var fixing = optionletPriceImp(optionType,
                effStrike,
                adjustedFixing(),
                stdDev);
            return fixing * coupon_.accrualPeriod() * discount_;
        }

        //! usually only need implement this (of course they may need
        //! to re-implement initialize too ...)
        protected virtual double optionletPriceImp(Option.Type optionType, double strike, double forward, double stdDev)
        {
            QLNet.Utils.QL_FAIL("you must implement this to get a vol-dependent price");
            return strike * forward * stdDev * (int)optionType;
        }
    }
}
