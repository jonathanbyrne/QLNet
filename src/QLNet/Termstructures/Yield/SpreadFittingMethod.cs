using QLNet.Math;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class SpreadFittingMethod : FittedBondDiscountCurve.FittingMethod
    {
        public SpreadFittingMethod(FittedBondDiscountCurve.FittingMethod method, Handle<YieldTermStructure> discountCurve)
            : base(method != null ? method.constrainAtZero() : true,
                method != null ? method.weights() : null,
                method != null ? method.optimizationMethod() : null)
        {
            method_ = method;
            discountingCurve_ = discountCurve;

            Utils.QL_REQUIRE(method != null, () => "Fitting method is empty");
            Utils.QL_REQUIRE(!discountingCurve_.empty(), () => "Discounting curve cannot be empty");
        }

        public override FittedBondDiscountCurve.FittingMethod clone() => MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;

        internal override void init()
        {
            //In case discount curve has a different reference date,
            //discount to this curve's reference date
            if (curve_.referenceDate() != discountingCurve_.link.referenceDate())
            {
                rebase_ = discountingCurve_.link.discount(curve_.referenceDate());
            }
            else
            {
                rebase_ = 1.0;
            }

            //Call regular init
            base.init();
        }

        public override int size() => method_.size();

        internal override double discountFunction(Vector x, double t) => method_.discount(x, t) * discountingCurve_.link.discount(t, true) / rebase_;

        // underlying parametric method
        private FittedBondDiscountCurve.FittingMethod method_;
        // adjustment in case underlying discount curve has different reference date
        private double rebase_;
        // discount curve from on top of which the spread will be calculated
        private Handle<YieldTermStructure> discountingCurve_;

    }
}