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

using QLNet.Cashflows;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Pricingengines.credit
{
    [JetBrains.Annotations.PublicAPI] public class IntegralCdsEngine : CreditDefaultSwap.Engine
    {
        public IntegralCdsEngine(Period step, Handle<DefaultProbabilityTermStructure> probability,
                                 double recoveryRate, Handle<YieldTermStructure> discountCurve, bool? includeSettlementDateFlows = null)
        {
            integrationStep_ = step;
            probability_ = probability;
            recoveryRate_ = recoveryRate;
            discountCurve_ = discountCurve;
            includeSettlementDateFlows_ = includeSettlementDateFlows;

            probability_.registerWith(update);
            discountCurve_.registerWith(update);
        }

        public override void calculate()
        {
            Utils.QL_REQUIRE(integrationStep_ != null, () => "null period set");
            Utils.QL_REQUIRE(!discountCurve_.empty(), () => "no discount term structure set");
            Utils.QL_REQUIRE(!probability_.empty(), () => "no probability term structure set");

            var today = Settings.evaluationDate();
            var settlementDate = discountCurve_.link.referenceDate();

            // Upfront Flow NPV. Either we are on-the-run (no flow)
            // or we are forward start
            var upfPVO1 = 0.0;
            if (!arguments_.upfrontPayment.hasOccurred(settlementDate, includeSettlementDateFlows_))
            {
                // date determining the probability survival so we have to pay
                // the upfront (did not knock out)
                var effectiveUpfrontDate =
                   arguments_.protectionStart > probability_.link.referenceDate() ?
                   arguments_.protectionStart : probability_.link.referenceDate();
                upfPVO1 =
                   probability_.link.survivalProbability(effectiveUpfrontDate) *
                   discountCurve_.link.discount(arguments_.upfrontPayment.date());
            }
            results_.upfrontNPV = upfPVO1 * arguments_.upfrontPayment.amount();

            results_.couponLegNPV = 0.0;
            results_.defaultLegNPV = 0.0;
            for (var i = 0; i < arguments_.leg.Count; ++i)
            {
                if (arguments_.leg[i].hasOccurred(settlementDate,
                                                  includeSettlementDateFlows_))
                    continue;

                var coupon = arguments_.leg[i] as FixedRateCoupon;

                // In order to avoid a few switches, we calculate the NPV
                // of both legs as a positive quantity. We'll give them
                // the right sign at the end.

                Date paymentDate = coupon.date(),
                     startDate = i == 0 ? arguments_.protectionStart :
                                  coupon.accrualStartDate(),
                                 endDate = coupon.accrualEndDate();
                var effectiveStartDate =
                   startDate <= today && today <= endDate ? today : startDate;
                var couponAmount = coupon.amount();

                var S = probability_.link.survivalProbability(paymentDate);

                // On one side, we add the fixed rate payments in case of
                // survival.
                results_.couponLegNPV +=
                   S * couponAmount * discountCurve_.link.discount(paymentDate);

                // On the other side, we add the payment (and possibly the
                // accrual) in case of default.

                var step = integrationStep_;
                var d0 = effectiveStartDate;
                var d1 = Date.Min(d0 + step, endDate);
                var P0 = probability_.link.defaultProbability(d0);
                var endDiscount = discountCurve_.link.discount(paymentDate);
                do
                {
                    var B =
                       arguments_.paysAtDefaultTime ?
                       discountCurve_.link.discount(d1) :
                       endDiscount;

                    var P1 = probability_.link.defaultProbability(d1);
                    var dP = P1 - P0;

                    // accrual...
                    if (arguments_.settlesAccrual)
                    {
                        if (arguments_.paysAtDefaultTime)
                            results_.couponLegNPV +=
                               coupon.accruedAmount(d1) * B * dP;
                        else
                            results_.couponLegNPV +=
                               couponAmount * B * dP;
                    }

                    // ...and claim.
                    var claim = arguments_.claim.amount(d1,
                                                           arguments_.notional.Value,
                                                           recoveryRate_);
                    results_.defaultLegNPV += claim * B * dP;

                    // setup for next time around the loop
                    P0 = P1;
                    d0 = d1;
                    d1 = Date.Min(d0 + step, endDate);
                }
                while (d0 < endDate);
            }

            var upfrontSign = 1.0;
            switch (arguments_.side)
            {
                case Protection.Side.Seller:
                    results_.defaultLegNPV *= -1.0;
                    break;
                case Protection.Side.Buyer:
                    results_.couponLegNPV *= -1.0;
                    results_.upfrontNPV *= -1.0;
                    upfrontSign = -1.0;
                    break;
                default:
                    Utils.QL_FAIL("unknown protection side");
                    break;
            }

            results_.value =
               results_.defaultLegNPV + results_.couponLegNPV + results_.upfrontNPV;
            results_.errorEstimate = null;

            if (results_.couponLegNPV.IsNotEqual(0.0))
            {
                results_.fairSpread =
                   -results_.defaultLegNPV * arguments_.spread / results_.couponLegNPV;
            }
            else
            {
                results_.fairSpread = null;
            }

            var upfrontSensitivity = upfPVO1 * arguments_.notional.Value;
            if (upfrontSensitivity.IsNotEqual(0.0))
            {
                results_.fairUpfront =
                   -upfrontSign * (results_.defaultLegNPV + results_.couponLegNPV)
                   / upfrontSensitivity;
            }
            else
            {
                results_.fairUpfront = null;
            }


            if (arguments_.spread.IsNotEqual(0.0))
            {
                results_.couponLegBPS =
                   results_.couponLegNPV * Const.BASIS_POINT / arguments_.spread;
            }
            else
            {
                results_.couponLegBPS = null;
            }

            if (arguments_.upfront.HasValue && arguments_.upfront.IsNotEqual(0.0))
            {
                results_.upfrontBPS =
                   results_.upfrontNPV * Const.BASIS_POINT / arguments_.upfront.Value;
            }
            else
            {
                results_.upfrontBPS = null;
            }
        }



        private Period integrationStep_;
        private Handle<DefaultProbabilityTermStructure> probability_;
        private double recoveryRate_;
        private Handle<YieldTermStructure> discountCurve_;
        private bool? includeSettlementDateFlows_;
    }
}
