namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class NonCentralChiSquareDistribution
    {
        private double df_, ncp_;

        public NonCentralChiSquareDistribution(double df, double ncp)
        {
            df_ = df;
            ncp_ = ncp;
        }

        public double value(double x)
        {
            if (x <= 0.0)
                return 0.0;

            var errmax = 1e-12;
            const int itrmax = 10000;
            var lam = 0.5 * ncp_;

            var u = System.Math.Exp(-lam);
            var v = u;
            var x2 = 0.5 * x;
            var f2 = 0.5 * df_;
            var f_x_2n = df_ - x;

            var t = 0.0;
            if (f2 * Const.QL_EPSILON > 0.125 &&
                System.Math.Abs(x2 - f2) < System.Math.Sqrt(Const.QL_EPSILON) * f2)
            {
                t = System.Math.Exp((1 - t) *
                                    (2 - t / (f2 + 1))) / System.Math.Sqrt(2.0 * Const.M_PI * (f2 + 1.0));
            }
            else
            {
                t = System.Math.Exp(f2 * System.Math.Log(x2) - x2 -
                                    GammaFunction.logValue(f2 + 1));
            }

            var ans = v * t;

            var flag = false;
            var n = 1;
            var f_2n = df_ + 2.0;
            f_x_2n += 2.0;

            double bound;
            for (; ; )
            {
                if (f_x_2n > 0)
                {
                    flag = true;
                    bound = t * x / f_x_2n;
                    if (bound <= errmax || n > itrmax)
                        goto L_End;
                }
                for (; ; )
                {
                    u *= lam / n;
                    v += u;
                    t *= x / f_2n;
                    ans += v * t;
                    n++;
                    f_2n += 2.0;
                    f_x_2n += 2.0;
                    if (!flag && n <= itrmax)
                        break;

                    bound = t * x / f_x_2n;
                    if (bound <= errmax || n > itrmax)
                        goto L_End;
                }
            }
            L_End:
            Utils.QL_REQUIRE(bound <= errmax, () => "didn't converge");
            return ans;
        }
    }
}