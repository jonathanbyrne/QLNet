using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Pricingengines.Bond;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    /// <summary>
    ///     Callable fixed rate bond class.
    /// </summary>
    [PublicAPI]
    public class CallableFixedRateBond : CallableBond
    {
        public CallableFixedRateBond(int settlementDays,
            double faceAmount,
            Schedule schedule,
            List<double> coupons,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100.0,
            Date issueDate = null,
            CallabilitySchedule putCallSchedule = null)
            : base(settlementDays, schedule, accrualDayCounter, issueDate, putCallSchedule)
        {
            frequency_ = schedule.tenor().frequency();

            var isZeroCouponBond = coupons.Count == 1 && Utils.close(coupons[0], 0.0);

            if (!isZeroCouponBond)
            {
                cashflows_ = new FixedRateLeg(schedule)
                    .withCouponRates(coupons, accrualDayCounter)
                    .withNotionals(faceAmount)
                    .withPaymentAdjustment(paymentConvention);

                addRedemptionsToCashflows(new List<double> { redemption });
            }
            else
            {
                var redemptionDate = calendar_.adjust(maturityDate_, paymentConvention);
                setSingleRedemption(faceAmount, redemption, redemptionDate);
            }

            // used for impliedVolatility() calculation
            var dummyVolQuote = new SimpleQuote(0.0);
            blackVolQuote_.linkTo(dummyVolQuote);
            blackEngine_ = new BlackCallableFixedRateBondEngine(blackVolQuote_, blackDiscountCurve_);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);
            var arguments = args as Arguments;

            Utils.QL_REQUIRE(arguments != null, () => "no arguments given");

            var settlement = arguments.settlementDate;

            arguments.redemption = redemption().amount();
            arguments.redemptionDate = redemption().date();

            var cfs = cashflows();

            arguments.couponDates = new List<Date>(cfs.Count - 1);
            arguments.couponAmounts = new List<double>(cfs.Count - 1);

            for (var i = 0; i < cfs.Count; i++)
            {
                if (!cfs[i].hasOccurred(settlement, false))
                {
                    if (cfs[i] is FixedRateCoupon)
                    {
                        arguments.couponDates.Add(cfs[i].date());
                        arguments.couponAmounts.Add(cfs[i].amount());
                    }
                }
            }

            arguments.callabilityPrices = new List<double>(putCallSchedule_.Count);
            arguments.callabilityDates = new List<Date>(putCallSchedule_.Count);
            arguments.paymentDayCounter = paymentDayCounter_;
            arguments.frequency = frequency_;
            arguments.putCallSchedule = putCallSchedule_;

            for (var i = 0; i < putCallSchedule_.Count; i++)
            {
                if (!putCallSchedule_[i].hasOccurred(settlement, false))
                {
                    arguments.callabilityDates.Add(putCallSchedule_[i].date());
                    arguments.callabilityPrices.Add(putCallSchedule_[i].price().amount());

                    if (putCallSchedule_[i].price().type() == Callability.Price.Type.Clean)
                    {
                        /* calling accrued() forces accrued interest to be zero
                           if future option date is also coupon date, so that dirty
                           price = clean price. Use here because callability is
                           always applied before coupon in the tree engine.
                        */
                        arguments.callabilityPrices[arguments.callabilityPrices.Count - 1] += accrued(putCallSchedule_[i].date());
                    }
                }
            }
        }

        /// <summary>
        ///     accrued interest used internally
        ///     <remarks>
        ///         accrued interest used internally, where includeToday = false
        ///         same as Bond::accruedAmount() but with enable early
        ///         payments true.  Forces accrued to be calculated in a
        ///         consistent way for future put/ call dates, which can be
        ///         problematic in lattice engines when option dates are also
        ///         coupon dates.
        ///     </remarks>
        /// </summary>
        /// <param name="settlement"></param>
        /// <returns></returns>
        private double accrued(Date settlement)
        {
            if (settlement == null)
            {
                settlement = settlementDate();
            }

            var IncludeToday = false;
            for (var i = 0; i < cashflows_.Count; ++i)
            {
                // the first coupon paying after d is the one we're after
                if (!cashflows_[i].hasOccurred(settlement, IncludeToday))
                {
                    var coupon = cashflows_[i] as Coupon;
                    if (coupon != null)
                        // !!!
                    {
                        return coupon.accruedAmount(settlement) /
                            notional(settlement) * 100.0;
                    }

                    return 0.0;
                }
            }

            return 0.0;
        }
    }
}
