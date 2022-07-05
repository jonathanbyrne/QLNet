using QLNet.Math;

namespace QLNet
{
    public static partial class Utils
    {
        public static double binomialCoefficient(int n, int k) => System.Math.Floor(0.5 + System.Math.Exp(binomialCoefficientLn(n, k)));

        public static double binomialCoefficientLn(int n, int k)
        {
            QL_REQUIRE(n >= k, () => "n<k not allowed");
            return Factorial.ln(n) - Factorial.ln(k) - Factorial.ln(n - k);
        }
        /*! Given an odd integer n and a real number z it returns p such that:
      1 - CumulativeBinomialDistribution((n-1)/2, n, p) =
                             CumulativeNormalDistribution(z)

      \pre n must be odd
      */

        public static double PeizerPrattMethod2Inversion(double z, int n)
        {
            QL_REQUIRE(n % 2 == 1, () => "n must be an odd number: " + n + " not allowed");

            var result = (z / (n + 1.0 / 3.0 + 0.1 / (n + 1.0)));
            result *= result;
            result = System.Math.Exp(-result * (n + 1.0 / 6.0));
            result = 0.5 + (z > 0 ? 1 : -1) * System.Math.Sqrt((0.25 * (1.0 - result)));
            return result;
        }
    }
}
