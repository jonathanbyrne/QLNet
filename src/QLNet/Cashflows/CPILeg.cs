using System.Collections.Generic;
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class CPILeg : CPILegBase
    {
        public CPILeg(Schedule schedule,
            ZeroInflationIndex index,
            double baseCPI,
            Period observationLag)
        {
            schedule_ = schedule;
            index_ = index;
            baseCPI_ = baseCPI;
            observationLag_ = observationLag;
            paymentDayCounter_ = new Thirty360();
            paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
            paymentCalendar_ = schedule.calendar();
            fixingDays_ = new List<int>() { 0 };
            observationInterpolation_ = InterpolationType.AsIndex;
            subtractInflationNominal_ = true;
            spreads_ = new List<double>() { 0 };
        }

        public override List<CashFlow> value()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var n = schedule_.Count - 1;
            var leg = new List<CashFlow>(n + 1);

            if (n > 0)
            {
                Utils.QL_REQUIRE(!fixedRates_.empty() || !spreads_.empty(), () => "no fixedRates or spreads given");

                Date refStart, start, refEnd, end;

                for (var i = 0; i < n; ++i)
                {
                    refStart = start = schedule_.date(i);
                    refEnd = end = schedule_.date(i + 1);
                    var paymentDate = paymentCalendar_.adjust(end, paymentAdjustment_);

                    Date exCouponDate = null;
                    if (exCouponPeriod_ != null)
                    {
                        exCouponDate = exCouponCalendar_.advance(paymentDate,
                            -exCouponPeriod_,
                            exCouponAdjustment_,
                            exCouponEndOfMonth_);
                    }

                    if (i == 0 && !schedule_.isRegular(i + 1))
                    {
                        var bdc = schedule_.businessDayConvention();
                        refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
                    }
                    if (i == n - 1 && !schedule_.isRegular(i + 1))
                    {
                        var bdc = schedule_.businessDayConvention();
                        refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
                    }
                    if (fixedRates_.Get(i, 1.0).IsEqual(0.0))
                    {
                        // fixed coupon
                        leg.Add(new FixedRateCoupon(paymentDate, notionals_.Get(i, 0.0),
                            Utils.effectiveFixedRate(spreads_, caps_, floors_, i),
                            paymentDayCounter_, start, end, refStart, refEnd, exCouponDate));
                    }
                    else
                    {
                        // zero inflation coupon
                        if (Utils.noOption(caps_, floors_, i))
                        {
                            // just swaplet
                            CPICoupon coup;

                            coup = new CPICoupon(baseCPI_,    // all have same base for ratio
                                paymentDate,
                                notionals_.Get(i, 0.0),
                                start, end,
                                fixingDays_.Get(i, 0),
                                index_, observationLag_,
                                observationInterpolation_,
                                paymentDayCounter_,
                                fixedRates_.Get(i, 0.0),
                                spreads_.Get(i, 0.0),
                                refStart, refEnd, exCouponDate);

                            // in this case you can set a pricer
                            // straight away because it only provides computation - not data
                            var pricer = new CPICouponPricer();
                            coup.setPricer(pricer);
                            leg.Add(coup);

                        }
                        else
                        {
                            // cap/floorlet
                            Utils.QL_FAIL("caps/floors on CPI coupons not implemented.");
                        }
                    }
                }
            }

            // in CPI legs you always have a notional flow of some sort
            var pDate = paymentCalendar_.adjust(schedule_.date(n), paymentAdjustment_);
            var fixingDate = pDate - observationLag_;
            CashFlow xnl = new CPICashFlow
            (notionals_.Get(n, 0.0), index_,
                new Date(), // is fake, i.e. you do not have one
                baseCPI_, fixingDate, pDate,
                subtractInflationNominal_, observationInterpolation_,
                index_.frequency());

            leg.Add(xnl);

            return leg;
        }
    }
}