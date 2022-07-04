using System;
using QLNet.Cashflows;
using QLNet.Patterns;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class PricerSetter : IAcyclicVisitor
    {
        private FloatingRateCouponPricer pricer_;
        public PricerSetter(FloatingRateCouponPricer pricer)
        {
            pricer_ = pricer;
        }

        public void visit(object o)
        {
            var types = new Type[] { o.GetType() };
            var methodInfo = Utils.GetMethodInfo(this, "visit", types);
            if (methodInfo != null)
            {
                methodInfo.Invoke(this, new object[] { o });
            }
        }

        public void visit(CashFlow c)
        {
            // nothing to do
        }
        public void visit(Coupon c)
        {
            // nothing to do
        }
        public void visit(FloatingRateCoupon c)
        {
            c.setPricer(pricer_);
        }
        public void visit(CappedFlooredCoupon c)
        {
            c.setPricer(pricer_);
        }
        public void visit(IborCoupon c)
        {
            var iborCouponPricer = pricer_ as IborCouponPricer;
            Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }
        public void visit(DigitalIborCoupon c)
        {
            var iborCouponPricer = pricer_ as IborCouponPricer;
            Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }
        public void visit(CappedFlooredIborCoupon c)
        {
            var iborCouponPricer = pricer_ as IborCouponPricer;
            Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }
        public void visit(CmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(CappedFlooredCmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(DigitalCmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(RangeAccrualFloatersCoupon c)
        {
            var rangeAccrualPricer = pricer_ as RangeAccrualPricer;
            Utils.QL_REQUIRE(rangeAccrualPricer != null, () => "pricer not compatible with range-accrual coupon");
            c.setPricer(rangeAccrualPricer);
        }
    }
}