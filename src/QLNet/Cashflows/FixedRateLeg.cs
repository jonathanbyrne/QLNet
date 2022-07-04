using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class FixedRateLeg : RateLegBase
    {
        // properties
        private List<InterestRate> couponRates_ = new List<InterestRate>();
        private DayCounter firstPeriodDC_, lastPeriodDC_;
        private Calendar calendar_;
        private Period exCouponPeriod_;
        private Calendar exCouponCalendar_;
        private BusinessDayConvention exCouponAdjustment_;
        private bool exCouponEndOfMonth_;

        // constructor
        public FixedRateLeg(Schedule schedule)
        {
            schedule_ = schedule;
            calendar_ = schedule.calendar();
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        // other initializers
        public FixedRateLeg withCouponRates(double couponRate, DayCounter paymentDayCounter) => withCouponRates(couponRate, paymentDayCounter, Compounding.Simple, Frequency.Annual);

        public FixedRateLeg withCouponRates(double couponRate, DayCounter paymentDayCounter, Compounding comp) => withCouponRates(couponRate, paymentDayCounter, comp, Frequency.Annual);

        public FixedRateLeg withCouponRates(double couponRate, DayCounter paymentDayCounter,
            Compounding comp, Frequency freq)
        {
            couponRates_.Clear();
            couponRates_.Add(new InterestRate(couponRate, paymentDayCounter, comp, freq));
            return this;
        }


        public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter) => withCouponRates(couponRates, paymentDayCounter, Compounding.Simple, Frequency.Annual);

        public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter, Compounding comp) => withCouponRates(couponRates, paymentDayCounter, comp, Frequency.Annual);

        public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter,
            Compounding comp, Frequency freq)
        {
            couponRates_.Clear();
            foreach (var r in couponRates)
                couponRates_.Add(new InterestRate(r, paymentDayCounter, comp, freq));
            return this;
        }

        public FixedRateLeg withCouponRates(InterestRate couponRate)
        {
            couponRates_.Clear();
            couponRates_.Add(couponRate);
            return this;
        }

        public FixedRateLeg withCouponRates(List<InterestRate> couponRates)
        {
            couponRates_ = couponRates;
            return this;
        }

        public FixedRateLeg withFirstPeriodDayCounter(DayCounter dayCounter)
        {
            firstPeriodDC_ = dayCounter;
            return this;
        }

        public FixedRateLeg withLastPeriodDayCounter(DayCounter dayCounter)
        {
            lastPeriodDC_ = dayCounter;
            return this;
        }

        public FixedRateLeg withPaymentCalendar(Calendar cal)
        {
            calendar_ = cal;
            return this;
        }

        public FixedRateLeg withExCouponPeriod(Period period, Calendar cal, BusinessDayConvention convention, bool endOfMonth = false)
        {
            exCouponPeriod_ = period;
            exCouponCalendar_ = cal;
            exCouponAdjustment_ = convention;
            exCouponEndOfMonth_ = endOfMonth;
            return this;
        }

        // creator
        public override List<CashFlow> value()
        {

            if (couponRates_.Count == 0)
                throw new ArgumentException("no coupon rates given");
            if (notionals_.Count == 0)
                throw new ArgumentException("no nominals given");

            var leg = new List<CashFlow>();

            var schCalendar = schedule_.calendar();

            // first period might be short or long
            Date start = schedule_[0], end = schedule_[1];
            var paymentDate = calendar_.adjust(end, paymentAdjustment_);
            Date exCouponDate = null;
            var rate = couponRates_[0];
            var nominal = notionals_[0];

            if (exCouponPeriod_ != null)
            {
                exCouponDate = exCouponCalendar_.advance(paymentDate,
                    -exCouponPeriod_,
                    exCouponAdjustment_,
                    exCouponEndOfMonth_);
            }
            if (schedule_.isRegular(1))
            {
                if (!(firstPeriodDC_ == null || firstPeriodDC_ == rate.dayCounter()))
                    throw new ArgumentException("regular first coupon does not allow a first-period day count");
                leg.Add(new FixedRateCoupon(paymentDate, nominal, rate, start, end, start, end, exCouponDate));
            }
            else
            {
                var refer = end - schedule_.tenor();
                refer = schCalendar.adjust(refer, schedule_.businessDayConvention());
                var r = new InterestRate(rate.rate(),
                    firstPeriodDC_ == null || firstPeriodDC_.empty() ? rate.dayCounter() : firstPeriodDC_,
                    rate.compounding(), rate.frequency());
                leg.Add(new FixedRateCoupon(paymentDate, nominal, r, start, end, refer, end, exCouponDate));
            }

            // regular periods
            for (var i = 2; i < schedule_.Count - 1; ++i)
            {
                start = end; end = schedule_[i];
                paymentDate = calendar_.adjust(end, paymentAdjustment_);
                if (exCouponPeriod_ != null)
                {
                    exCouponDate = exCouponCalendar_.advance(paymentDate,
                        -exCouponPeriod_,
                        exCouponAdjustment_,
                        exCouponEndOfMonth_);
                }
                if (i - 1 < couponRates_.Count)
                    rate = couponRates_[i - 1];
                else
                    rate = couponRates_.Last();
                if (i - 1 < notionals_.Count)
                    nominal = notionals_[i - 1];
                else
                    nominal = notionals_.Last();

                leg.Add(new FixedRateCoupon(paymentDate, nominal, rate, start, end, start, end, exCouponDate));
            }

            if (schedule_.Count > 2)
            {
                // last period might be short or long
                var N = schedule_.Count;
                start = end; end = schedule_[N - 1];
                paymentDate = calendar_.adjust(end, paymentAdjustment_);
                if (exCouponPeriod_ != null)
                {
                    exCouponDate = exCouponCalendar_.advance(paymentDate,
                        -exCouponPeriod_,
                        exCouponAdjustment_,
                        exCouponEndOfMonth_);
                }

                if (N - 2 < couponRates_.Count)
                    rate = couponRates_[N - 2];
                else
                    rate = couponRates_.Last();
                if (N - 2 < notionals_.Count)
                    nominal = notionals_[N - 2];
                else
                    nominal = notionals_.Last();

                var r = new InterestRate(rate.rate(),
                    lastPeriodDC_ == null ? rate.dayCounter() : lastPeriodDC_, rate.compounding(), rate.frequency());
                if (schedule_.isRegular(N - 1))
                    leg.Add(new FixedRateCoupon(paymentDate, nominal, r, start, end, start, end, exCouponDate));
                else
                {
                    var refer = start + schedule_.tenor();
                    refer = schCalendar.adjust(refer, schedule_.businessDayConvention());
                    leg.Add(new FixedRateCoupon(paymentDate, nominal, r, start, end, start, refer, exCouponDate));
                }
            }
            return leg;
        }
    }
}