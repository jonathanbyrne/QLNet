using JetBrains.Annotations;
using QLNet.Math.integrals;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class BivariateCumulativeNormalDistributionWe04DP
    {
        private double correlation_;
        private CumulativeNormalDistribution cumnorm_ = new CumulativeNormalDistribution();

        public BivariateCumulativeNormalDistributionWe04DP(double rho)
        {
            correlation_ = rho;
            QLNet.Utils.QL_REQUIRE(rho >= -1.0, () => "rho must be >= -1.0 (" + rho + " not allowed)");
            QLNet.Utils.QL_REQUIRE(rho <= 1.0, () => "rho must be <= 1.0 (" + rho + " not allowed)");
        }

        // function
        public double value(double x, double y)
        {
            /* The implementation is described at section 2.4 "Hybrid
               Numerical Integration Algorithms" of "Numerical Computation
               of Rectangular Bivariate an Trivariate Normal and t
               Probabilities", Genz (2004), Statistics and Computing 14,
               151-160. (available at
               www.sci.wsu.edu/math/faculty/henz/homepage)

               The Gauss-Legendre quadrature have been extracted to
               TabulatedGaussLegendre (x,w zero-based)

               Tthe functions ot be integrated numerically have been moved
               to classes eqn3 and eqn6

               Change some magic numbers to M_PI */

            var gaussLegendreQuad = new TabulatedGaussLegendre();
            if (System.Math.Abs(correlation_) < 0.3)
            {
                gaussLegendreQuad.order(6);
            }
            else if (System.Math.Abs(correlation_) < 0.75)
            {
                gaussLegendreQuad.order(12);
            }

            var h = -x;
            var k = -y;
            var hk = h * k;
            var BVN = 0.0;

            if (System.Math.Abs(correlation_) < 0.925)
            {
                if (System.Math.Abs(correlation_) > 0)
                {
                    var asr = System.Math.Asin(correlation_);
                    var f = new eqn3(h, k, asr);
                    BVN = gaussLegendreQuad.value(f.value);
                    BVN *= asr * (0.25 / Const.M_PI);
                }

                BVN += cumnorm_.value(-h) * cumnorm_.value(-k);
            }
            else
            {
                if (correlation_ < 0)
                {
                    k *= -1;
                    hk *= -1;
                }

                if (System.Math.Abs(correlation_) < 1)
                {
                    var Ass = (1 - correlation_) * (1 + correlation_);
                    var a = System.Math.Sqrt(Ass);
                    var bs = (h - k) * (h - k);
                    var c = (4 - hk) / 8;
                    var d = (12 - hk) / 16;
                    var asr = -(bs / Ass + hk) / 2;
                    if (asr > -100)
                    {
                        BVN = a * System.Math.Exp(asr) *
                              (1 - c * (bs - Ass) * (1 - d * bs / 5) / 3 +
                               c * d * Ass * Ass / 5);
                    }

                    if (-hk < 100)
                    {
                        var B = System.Math.Sqrt(bs);
                        BVN -= System.Math.Exp(-hk / 2) * 2.506628274631 *
                               cumnorm_.value(-B / a) * B *
                               (1 - c * bs * (1 - d * bs / 5) / 3);
                    }

                    a /= 2;
                    var f = new eqn6(a, c, d, bs, hk);
                    BVN += gaussLegendreQuad.value(f.value);
                    BVN /= -2.0 * Const.M_PI;
                }

                if (correlation_ > 0)
                {
                    BVN += cumnorm_.value(-System.Math.Max(h, k));
                }
                else
                {
                    BVN *= -1;
                    if (k > h)
                    {
                        // evaluate cumnorm where it is most precise, that
                        // is in the lower tail because of double accuracy
                        // around 0.0 vs around 1.0
                        if (h >= 0)
                        {
                            BVN += cumnorm_.value(-h) - cumnorm_.value(-k);
                        }
                        else
                        {
                            BVN += cumnorm_.value(k) - cumnorm_.value(h);
                        }
                    }
                }
            }

            return BVN;
        }
    }
}
