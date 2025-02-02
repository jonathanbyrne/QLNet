﻿//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Math.Interpolations;
using QLNet.Termstructures;
using QLNet.Termstructures.Credit;
using QLNet.Termstructures.Yield;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.PricingEngines.credit
{
    [PublicAPI]
    public class IsdaCdsEngine : CreditDefaultSwap.Engine
    {
        public enum AccrualBias
        {
            HalfDayBias, // as in [1] formula (50), second (error) term is
            // included
            NoBias // as in [1], but second term in formula (50) is not included
        }

        public enum ForwardsInCouponPeriod
        {
            Flat, // as in [1], formula (52), second (error) term is included
            Piecewise // as in [1], but second term in formula (52) is not
            // included
        }

        public enum NumericalFix
        {
            None, // as in [1] footnote 26 (i.e. 10^{-50} is added to
            // denominators $f_i+h_i$$)
            Taylor // as in [2] i.e. for $f_i+h_i < 10^{-4}$ a Taylor expansion
            // is used to avoid zero denominators
        }

        private AccrualBias accrualBias_;
        private Handle<YieldTermStructure> discountCurve_;
        private ForwardsInCouponPeriod forwardsInCouponPeriod_;
        private bool? includeSettlementDateFlows_;
        private NumericalFix numericalFix_;
        private Handle<DefaultProbabilityTermStructure> probability_;
        private double recoveryRate_;
        /*! Constructor where the client code is responsible for providing a
           default curve and an interest rate curve compliant with the ISDA
           specifications.

           To be precisely consistent with the ISDA specification
           QL_USE_INDEXED_COUPON
           must not be defined. This is not checked in order not to
           kill the engine completely in this case.

           Furthermore, the ibor index in the swap rate helpers should not
           provide the evaluation date's fixing.
        */

        public IsdaCdsEngine(Handle<DefaultProbabilityTermStructure> probability,
            double recoveryRate,
            Handle<YieldTermStructure> discountCurve,
            bool? includeSettlementDateFlows = null,
            NumericalFix numericalFix = NumericalFix.Taylor,
            AccrualBias accrualBias = AccrualBias.HalfDayBias,
            ForwardsInCouponPeriod forwardsInCouponPeriod = ForwardsInCouponPeriod.Piecewise)
        {
            probability_ = probability;
            recoveryRate_ = recoveryRate;
            discountCurve_ = discountCurve;
            includeSettlementDateFlows_ = includeSettlementDateFlows;
            numericalFix_ = numericalFix;
            accrualBias_ = accrualBias;
            forwardsInCouponPeriod_ = forwardsInCouponPeriod;

            probability_.registerWith(update);
            discountCurve_.registerWith(update);
        }

        public override void calculate()
        {
            QLNet.Utils.QL_REQUIRE(numericalFix_ == NumericalFix.None || numericalFix_ == NumericalFix.Taylor, () =>
                "numerical fix must be None or Taylor");
            QLNet.Utils.QL_REQUIRE(accrualBias_ == AccrualBias.HalfDayBias || accrualBias_ == AccrualBias.NoBias, () =>
                "accrual bias must be HalfDayBias or NoBias");
            QLNet.Utils.QL_REQUIRE(forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Flat ||
                                   forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Piecewise, () =>
                "forwards in coupon period must be Flat or Piecewise");

            // it would be possible to handle the cases which are excluded below,
            // but the ISDA engine is not explicitly specified to handle them,
            // so we just forbid them too

            var dc = new Actual365Fixed();
            var dc1 = new Actual360();
            var dc2 = new Actual360(true);

            var evalDate = Settings.evaluationDate();

            // check if given curves are ISDA compatible
            // (the interpolation is checked below)

            QLNet.Utils.QL_REQUIRE(!discountCurve_.empty(), () => "no discount term structure set");
            QLNet.Utils.QL_REQUIRE(!probability_.empty(), () => "no probability term structure set");
            QLNet.Utils.QL_REQUIRE(discountCurve_.link.dayCounter() == dc, () =>
                "yield term structure day counter (" + discountCurve_.link.dayCounter() + ") should be Act/365(Fixed)");
            QLNet.Utils.QL_REQUIRE(probability_.link.dayCounter() == dc, () =>
                "probability term structure day counter (" + probability_.link.dayCounter() + ") should be "
                + "Act/365(Fixed)");
            QLNet.Utils.QL_REQUIRE(discountCurve_.link.referenceDate() == evalDate, () =>
                "yield term structure reference date (" + discountCurve_.link.referenceDate()
                                                        + " should be evaluation date (" + evalDate + ")");
            QLNet.Utils.QL_REQUIRE(probability_.link.referenceDate() == evalDate, () =>
                "probability term structure reference date (" + probability_.link.referenceDate()
                                                              + " should be evaluation date (" + evalDate + ")");
            QLNet.Utils.QL_REQUIRE(arguments_.settlesAccrual, () => "ISDA engine not compatible with non accrual paying CDS");
            QLNet.Utils.QL_REQUIRE(arguments_.paysAtDefaultTime, () => "ISDA engine not compatible with end period payment");
            QLNet.Utils.QL_REQUIRE(arguments_.claim as FaceValueClaim != null, () =>
                "ISDA engine not compatible with non face value claim");

            var maturity = arguments_.maturity;
            var effectiveProtectionStart = Date.Max(arguments_.protectionStart, evalDate + 1);

            // collect nodes from both curves and sort them
            List<Date> yDates = new List<Date>(), cDates = new List<Date>();
            if (discountCurve_.link is PiecewiseYieldCurve<Discount, LogLinear> castY1)
            {
                if (castY1.dates() != null)
                {
                    yDates = castY1.dates();
                }
            }
            else if (discountCurve_.link is InterpolatedForwardCurve<BackwardFlat> castY2)
            {
                yDates = castY2.dates();
            }
            else if (discountCurve_.link is InterpolatedForwardCurve<ForwardFlat> castY3)
            {
                yDates = castY3.dates();
            }
            else if (discountCurve_.link is FlatForward castY4)
            {
            }
            else
            {
                QLNet.Utils.QL_FAIL("Yield curve must be flat forward interpolated");
            }

            if (probability_.link is InterpolatedSurvivalProbabilityCurve<LogLinear> castC1)
            {
                cDates = castC1.dates();
            }
            else if (probability_.link is InterpolatedHazardRateCurve<BackwardFlat> castC2)
            {
                cDates = castC2.dates();
            }
            else if (probability_.link is FlatHazardRate castC3)
            {
            }
            else
            {
                QLNet.Utils.QL_FAIL("Credit curve must be flat forward interpolated");
            }

            // Todo check
            var nodes = yDates.Union(cDates).ToList();

            if (nodes.empty())
            {
                nodes.Add(maturity);
            }

            var nFix = numericalFix_ == NumericalFix.None ? 1E-50 : 0.0;

            // protection leg pricing (npv is always negative at this stage)
            var protectionNpv = 0.0;

            var d0 = effectiveProtectionStart - 1;
            var P0 = discountCurve_.link.discount(d0);
            var Q0 = probability_.link.survivalProbability(d0);
            Date d1;
            var result = nodes.FindIndex(item => item > effectiveProtectionStart);

            for (var it = result; it < nodes.Count; ++it)
            {
                if (nodes[it] > maturity)
                {
                    d1 = maturity;
                    it = nodes.Count - 1; //early exit
                }
                else
                {
                    d1 = nodes[it];
                }

                var P1 = discountCurve_.link.discount(d1);
                var Q1 = probability_.link.survivalProbability(d1);

                var fhat = System.Math.Log(P0) - System.Math.Log(P1);
                var hhat = System.Math.Log(Q0) - System.Math.Log(Q1);
                var fhphh = fhat + hhat;

                if (fhphh < 1E-4 && numericalFix_ == NumericalFix.Taylor)
                {
                    var fhphhq = fhphh * fhphh;
                    protectionNpv +=
                        P0 * Q0 * hhat * (1.0 - 0.5 * fhphh + 1.0 / 6.0 * fhphhq -
                                          1.0 / 24.0 * fhphhq * fhphh +
                                          1.0 / 120 * fhphhq * fhphhq);
                }
                else
                {
                    protectionNpv += hhat / (fhphh + nFix) * (P0 * Q0 - P1 * Q1);
                }

                d0 = d1;
                P0 = P1;
                Q0 = Q1;
            }

            protectionNpv *= arguments_.claim.amount(null, arguments_.notional.Value, recoveryRate_);

            results_.defaultLegNPV = protectionNpv;

            // premium leg pricing (npv is always positive at this stage)

            double premiumNpv = 0.0, defaultAccrualNpv = 0.0;
            for (var i = 0; i < arguments_.leg.Count; ++i)
            {
                var coupon = arguments_.leg[i] as FixedRateCoupon;

                QLNet.Utils.QL_REQUIRE(coupon.dayCounter() == dc ||
                                       coupon.dayCounter() == dc1 ||
                                       coupon.dayCounter() == dc2, () =>
                    "ISDA engine requires a coupon day counter Act/365Fixed "
                    + "or Act/360 (" + coupon.dayCounter() + ")");

                // premium coupons

                if (!arguments_.leg[i].hasOccurred(evalDate, includeSettlementDateFlows_))
                {
                    var x1 = coupon.amount();
                    var x2 = discountCurve_.link.discount(coupon.date());
                    var x3 = probability_.link.survivalProbability(coupon.date() - 1);

                    premiumNpv +=
                        coupon.amount() *
                        discountCurve_.link.discount(coupon.date()) *
                        probability_.link.survivalProbability(coupon.date() - 1);
                }

                // default accruals

                if (!new simple_event(coupon.accrualEndDate())
                        .hasOccurred(effectiveProtectionStart, false))
                {
                    var start = Date.Max(coupon.accrualStartDate(), effectiveProtectionStart) - 1;
                    var end = coupon.date() - 1;
                    var tstart = discountCurve_.link.timeFromReference(coupon.accrualStartDate() - 1) -
                                 (accrualBias_ == AccrualBias.HalfDayBias ? 1.0 / 730.0 : 0.0);
                    var localNodes = new List<Date>();
                    localNodes.Add(start);
                    //add intermediary nodes, if any
                    if (forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Piecewise)
                    {
                        foreach (var node in nodes)
                        {
                            if (node > start && node < end)
                            {
                                localNodes.Add(node);
                            }
                        }
                        //std::vector<Date>::const_iterator it0 = std::upper_bound(nodes.begin(), nodes.end(), start);
                        //std::vector<Date>::const_iterator it1 = std::lower_bound(nodes.begin(), nodes.end(), end);
                        //localNodes.insert(localNodes.end(), it0, it1);
                    }

                    localNodes.Add(end);

                    var defaultAccrThisNode = 0.0;
                    var firstnode = localNodes.First();
                    var t0 = discountCurve_.link.timeFromReference(firstnode);
                    P0 = discountCurve_.link.discount(firstnode);
                    Q0 = probability_.link.survivalProbability(firstnode);

                    foreach (var node in localNodes.Skip(1)) //for (++node; node != localNodes.Last(); ++node)
                    {
                        var t1 = discountCurve_.link.timeFromReference(node);
                        var P1 = discountCurve_.link.discount(node);
                        var Q1 = probability_.link.survivalProbability(node);
                        var fhat = System.Math.Log(P0) - System.Math.Log(P1);
                        var hhat = System.Math.Log(Q0) - System.Math.Log(Q1);
                        var fhphh = fhat + hhat;
                        if (fhphh < 1E-4 && numericalFix_ == NumericalFix.Taylor)
                        {
                            // see above, terms up to (f+h)^3 seem more than enough,
                            // what exactly is implemented in the standard isda C
                            // code ?
                            var fhphhq = fhphh * fhphh;
                            defaultAccrThisNode +=
                                hhat * P0 * Q0 *
                                ((t0 - tstart) *
                                 (1.0 - 0.5 * fhphh + 1.0 / 6.0 * fhphhq -
                                  1.0 / 24.0 * fhphhq * fhphh) +
                                 (t1 - t0) *
                                 (0.5 - 1.0 / 3.0 * fhphh + 1.0 / 8.0 * fhphhq -
                                  1.0 / 30.0 * fhphhq * fhphh));
                        }
                        else
                        {
                            defaultAccrThisNode +=
                                hhat / (fhphh + nFix) *
                                ((t1 - t0) * ((P0 * Q0 - P1 * Q1) / (fhphh + nFix) -
                                              P1 * Q1) +
                                 (t0 - tstart) * (P0 * Q0 - P1 * Q1));
                        }

                        t0 = t1;
                        P0 = P1;
                        Q0 = Q1;
                    }

                    defaultAccrualNpv += defaultAccrThisNode * arguments_.notional.Value *
                        coupon.rate() * 365.0 / 360.0;
                }
            }

            results_.couponLegNPV = premiumNpv + defaultAccrualNpv;

            // upfront flow npv

            var upfPVO1 = 0.0;
            results_.upfrontNPV = 0.0;
            if (!arguments_.upfrontPayment.hasOccurred(
                    evalDate, includeSettlementDateFlows_))
            {
                upfPVO1 =
                    discountCurve_.link.discount(arguments_.upfrontPayment.date());
                if (arguments_.upfrontPayment.amount().IsNotEqual(0.0))
                {
                    results_.upfrontNPV = upfPVO1 * arguments_.upfrontPayment.amount();
                }
            }

            results_.accrualRebateNPV = 0.0;
            if (arguments_.accrualRebate != null &&
                arguments_.accrualRebate.amount().IsNotEqual(0.0) &&
                !arguments_.accrualRebate.hasOccurred(evalDate, includeSettlementDateFlows_))
            {
                results_.accrualRebateNPV =
                    discountCurve_.link.discount(arguments_.accrualRebate.date()) *
                    arguments_.accrualRebate.amount();
            }

            double upfrontSign = 1;

            if (arguments_.side == Protection.Side.Seller)
            {
                results_.defaultLegNPV *= -1.0;
                results_.accrualRebateNPV *= -1.0;
            }
            else
            {
                results_.couponLegNPV *= -1.0;
                results_.upfrontNPV *= -1.0;
            }

            results_.value = results_.defaultLegNPV + results_.couponLegNPV +
                             results_.upfrontNPV + results_.accrualRebateNPV;

            results_.errorEstimate = null;

            if (results_.couponLegNPV.IsNotEqual(0.0))
            {
                results_.fairSpread =
                    -results_.defaultLegNPV * arguments_.spread /
                    (results_.couponLegNPV + results_.accrualRebateNPV);
            }
            else
            {
                results_.fairSpread = null;
            }

            var upfrontSensitivity = upfPVO1 * arguments_.notional.Value;
            if (upfrontSensitivity.IsNotEqual(0.0))
            {
                results_.fairUpfront =
                    -upfrontSign * (results_.defaultLegNPV + results_.couponLegNPV +
                                    results_.accrualRebateNPV) /
                    upfrontSensitivity;
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

            if (arguments_.upfront != null && arguments_.upfront.IsNotEqual(0.0))
            {
                results_.upfrontBPS =
                    results_.upfrontNPV * Const.BASIS_POINT / arguments_.upfront;
            }
            else
            {
                results_.upfrontBPS = null;
            }
        }

        public Handle<DefaultProbabilityTermStructure> isdaCreditCurve() => probability_;

        public Handle<YieldTermStructure> isdaRateCurve() => discountCurve_;
    }
}
