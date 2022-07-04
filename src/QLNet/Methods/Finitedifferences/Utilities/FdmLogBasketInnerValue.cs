using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    [JetBrains.Annotations.PublicAPI] public class FdmLogBasketInnerValue : FdmInnerValueCalculator
    {
        public FdmLogBasketInnerValue(BasketPayoff payoff,
            FdmMesher mesher)
        {
            payoff_ = payoff;
            mesher_ = mesher;
        }

        public override double innerValue(FdmLinearOpIterator iter, double t)
        {
            var x = new Vector(mesher_.layout().dim().Count);
            for (var i = 0; i < x.size(); ++i)
            {
                x[i] = System.Math.Exp(mesher_.location(iter, i));
            }

            return payoff_.value(x);
        }
        public override double avgInnerValue(FdmLinearOpIterator iter, double t) => innerValue(iter, t);

        protected BasketPayoff payoff_;
        protected FdmMesher mesher_;
    }
}