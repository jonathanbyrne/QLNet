using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class TabulatedGaussLegendre
    {
        public TabulatedGaussLegendre(int n = 20) { order(n); }

        public double value(Func<double, double> f)
        {
            Utils.QL_REQUIRE(w_ != null, () => "Null weights");
            Utils.QL_REQUIRE(x_ != null, () => "Null abscissas");
            int startIdx;
            double val;

            var isOrderOdd = order_ & 1;

            if (isOrderOdd > 0)
            {
                Utils.QL_REQUIRE(n_ > 0, () => "assume at least 1 point in quadrature");
                val = w_[0] * f(x_[0]);
                startIdx = 1;
            }
            else
            {
                val = 0.0;
                startIdx = 0;
            }

            for (var i = startIdx; i < n_; ++i)
            {
                val += w_[i] * f(x_[i]);
                val += w_[i] * f(-x_[i]);
            }
            return val;
        }

        public void order(int order)
        {
            switch (order)
            {
                case 6:
                    order_ = order; x_ = x6.ToList(); w_ = w6.ToList(); n_ = n6;
                    break;
                case 7:
                    order_ = order; x_ = x7.ToList(); w_ = w7.ToList(); n_ = n7;
                    break;
                case 12:
                    order_ = order; x_ = x12.ToList(); w_ = w12.ToList(); n_ = n12;
                    break;
                case 20:
                    order_ = order; x_ = x20.ToList(); w_ = w20.ToList(); n_ = n20;
                    break;
                default:
                    Utils.QL_FAIL("order " + order + " not supported");
                    break;
            }
        }

        public int order() => order_;

        private int order_;

        private List<double> w_;
        private List<double> x_;
        private int n_;

        private static double[] w6 = { 0.467913934572691, 0.360761573048139, 0.171324492379170 };
        private static double[] x6 = { 0.238619186083197, 0.661209386466265, 0.932469514203152 };
        private static int n6 = 3;

        private static double[] w7 = { 0.417959183673469, 0.381830050505119, 0.279705391489277, 0.129484966168870 };
        private static double[] x7 = { 0.000000000000000, 0.405845151377397, 0.741531185599394, 0.949107912342759 };
        private static int n7 = 4;

        private static double[] w12 = { 0.249147045813403, 0.233492536538355, 0.203167426723066, 0.160078328543346,
            0.106939325995318, 0.047175336386512
        };
        private static double[] x12 = { 0.125233408511469, 0.367831498998180, 0.587317954286617, 0.769902674194305,
            0.904117256370475, 0.981560634246719
        };
        private static int n12 = 6;

        private static double[] w20 = { 0.152753387130726, 0.149172986472604, 0.142096109318382, 0.131688638449177,
            0.118194531961518, 0.101930119817240, 0.083276741576704, 0.062672048334109,
            0.040601429800387, 0.017614007139152
        };
        private static double[] x20 = { 0.076526521133497, 0.227785851141645, 0.373706088715420, 0.510867001950827,
            0.636053680726515, 0.746331906460151, 0.839116971822219, 0.912234428251326,
            0.963971927277914, 0.993128599185095
        };
        private static int n20 = 10;
    }
}