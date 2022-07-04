namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class InverseCumulativeNormal : IValue
    {
        double average_, sigma_;

        // Coefficients for the rational approximation.
        const double a1_ = -3.969683028665376e+01;
        const double a2_ = 2.209460984245205e+02;
        const double a3_ = -2.759285104469687e+02;
        const double a4_ = 1.383577518672690e+02;
        const double a5_ = -3.066479806614716e+01;
        const double a6_ = 2.506628277459239e+00;

        const double b1_ = -5.447609879822406e+01;
        const double b2_ = 1.615858368580409e+02;
        const double b3_ = -1.556989798598866e+02;
        const double b4_ = 6.680131188771972e+01;
        const double b5_ = -1.328068155288572e+01;

        const double c1_ = -7.784894002430293e-03;
        const double c2_ = -3.223964580411365e-01;
        const double c3_ = -2.400758277161838e+00;
        const double c4_ = -2.549732539343734e+00;
        const double c5_ = 4.374664141464968e+00;
        const double c6_ = 2.938163982698783e+00;

        const double d1_ = 7.784695709041462e-03;
        const double d2_ = 3.224671290700398e-01;
        const double d3_ = 2.445134137142996e+00;
        const double d4_ = 3.754408661907416e+00;

        // Limits of the approximation regions
        const double x_low_ = 0.02425;
        const double x_high_ = 1.0 - x_low_;


        public InverseCumulativeNormal() : this(0.0, 1.0) { }
        public InverseCumulativeNormal(double average, double sigma)
        {
            average_ = average;
            sigma_ = sigma;

            Utils.QL_REQUIRE(sigma_ > 0.0, () => "sigma must be greater than 0.0 (" + sigma_ + " not allowed)");
        }

        // function
        public double value(double x)
        {
            if (x < 0.0 || x > 1.0)
            {
                // try to recover if due to numerical error
                if (Utils.close_enough(x, 1.0))
                {
                    x = 1.0;
                }
                else if (System.Math.Abs(x) < Const.QL_EPSILON)
                {
                    x = 0.0;
                }
                else
                {
                    Utils.QL_FAIL("InverseCumulativeNormal(" + x + ") undefined: must be 0 < x < 1");
                }
            }

            double z, r;

            if (x < x_low_)
            {
                // Rational approximation for the lower region 0<x<u_low
                z = System.Math.Sqrt(-2.0 * System.Math.Log(x));
                z = (((((c1_ * z + c2_) * z + c3_) * z + c4_) * z + c5_) * z + c6_) /
                    ((((d1_ * z + d2_) * z + d3_) * z + d4_) * z + 1.0);
            }
            else if (x <= x_high_)
            {
                // Rational approximation for the central region u_low<=x<=u_high
                z = x - 0.5;
                r = z * z;
                z = (((((a1_ * r + a2_) * r + a3_) * r + a4_) * r + a5_) * r + a6_) * z /
                    (((((b1_ * r + b2_) * r + b3_) * r + b4_) * r + b5_) * r + 1.0);
            }
            else
            {
                // Rational approximation for the upper region u_high<x<1
                z = System.Math.Sqrt(-2.0 * System.Math.Log(1.0 - x));
                z = -(((((c1_ * z + c2_) * z + c3_) * z + c4_) * z + c5_) * z + c6_) /
                    ((((d1_ * z + d2_) * z + d3_) * z + d4_) * z + 1.0);
            }


            // The relative error of the approximation has absolute value less
            // than 1.15e-9.  One iteration of Halley's rational method (third
            // order) gives full machine precision.
            // #define REFINE_TO_FULL_MACHINE_PRECISION_USING_HALLEYS_METHOD
#if REFINE_TO_FULL_MACHINE_PRECISION_USING_HALLEYS_METHOD
         private static readonly CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
         // error (f_(z) - x) divided by the cumulative's derivative
         r = (f_(z) - x) * M_SQRT2 * M_SQRTPI * exp(0.5 * z * z);
         //  Halley's method
         z -= r / (1 + 0.5 * z * r);
#endif

            return average_ + z * sigma_;
        }
    }
}