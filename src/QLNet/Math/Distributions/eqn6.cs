namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class eqn6
    {
        /* Relates to eqn6 Genz 2004 */
        public eqn6(double a, double c, double d, double bs, double hk)
        {
            a_ = a;
            c_ = c;
            d_ = d;
            bs_ = bs;
            hk_ = hk;
        }

        public double value(double x)
        {
            var xs = a_ * (-x + 1);
            xs = System.Math.Abs(xs * xs);
            var rs = System.Math.Sqrt(1 - xs);
            var asr = -(bs_ / xs + hk_) / 2;
            if (asr > -100.0)
            {
                return a_ * System.Math.Exp(asr) *
                       (System.Math.Exp(-hk_ * (1 - rs) / (2 * (1 + rs))) / rs -
                        (1 + c_ * xs * (1 + d_ * xs)));
            }
            else
            {
                return 0.0;
            }

        }

        private double a_, c_, d_, bs_, hk_;

    }
}