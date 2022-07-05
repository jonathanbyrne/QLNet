using JetBrains.Annotations;

namespace QLNet.Methods.montecarlo
{
    [PublicAPI]
    public class MonomialFct : IValue
    {
        private int order_;

        public MonomialFct(int order)
        {
            order_ = order;
        }

        public double value(double x)
        {
            var ret = 1.0;
            for (var i = 0; i < order_; ++i)
            {
                ret *= x;
            }

            return ret;
        }
    }
}
