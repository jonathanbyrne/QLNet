using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class PricerSetter : IAcyclicVisitor
    {
        private FloatingRateCouponPricer pricer_;

        public PricerSetter(FloatingRateCouponPricer pricer)
        {
            pricer_ = pricer;
        }

        public void visit(object o)
        {
            var types = new[] { o.GetType() };
            var methodInfo = QLNet.Utils.GetMethodInfo(this, "visit", types);
            if (methodInfo != null)
            {
                methodInfo.Invoke(this, new[] { o });
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
            QLNet.Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }

        public void visit(DigitalIborCoupon c)
        {
            var iborCouponPricer = pricer_ as IborCouponPricer;
            QLNet.Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }

        public void visit(CappedFlooredIborCoupon c)
        {
            var iborCouponPricer = pricer_ as IborCouponPricer;
            QLNet.Utils.QL_REQUIRE(iborCouponPricer != null, () => "pricer not compatible with Ibor coupon");
            c.setPricer(iborCouponPricer);
        }

        public void visit(CmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            QLNet.Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(CappedFlooredCmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            QLNet.Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(DigitalCmsCoupon c)
        {
            var cmsCouponPricer = pricer_ as CmsCouponPricer;
            QLNet.Utils.QL_REQUIRE(cmsCouponPricer != null, () => "pricer not compatible with CMS coupon");
            c.setPricer(cmsCouponPricer);
        }

        public void visit(RangeAccrualFloatersCoupon c)
        {
            var rangeAccrualPricer = pricer_ as RangeAccrualPricer;
            QLNet.Utils.QL_REQUIRE(rangeAccrualPricer != null, () => "pricer not compatible with range-accrual coupon");
            c.setPricer(rangeAccrualPricer);
        }
    }
}
