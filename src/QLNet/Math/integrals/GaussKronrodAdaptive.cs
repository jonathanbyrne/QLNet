using System;
using QLNet.Exceptions;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class GaussKronrodAdaptive : Integrator
    {
        public GaussKronrodAdaptive(double absoluteAccuracy, int maxEvaluations) : base(absoluteAccuracy, maxEvaluations)
        {
            Utils.QL_REQUIRE(maxEvaluations >= 15, () =>
                "required maxEvaluations (" + maxEvaluations + ") not allowed. It must be >= 15");
        }
        protected override double integrate(Func<double, double> f, double a, double b) => integrateRecursively(f, a, b, absoluteAccuracy().GetValueOrDefault());

        private double integrateRecursively(Func<double, double> f, double a, double b, double tolerance)
        {

            var halflength = (b - a) / 2;
            var center = (a + b) / 2;

            double g7; // will be result of G7 integral
            double k15; // will be result of K15 integral

            double t; // t (abscissa) and f(t)
            double fsum;
            var fc = f(center);
            g7 = fc * KronrodintegralArrays.g7w[0];
            k15 = fc * KronrodintegralArrays.k15w[0];

            // calculate g7 and half of k15
            int j;
            int j2;
            for (j = 1, j2 = 2; j < 4; j++, j2 += 2)
            {
                t = halflength * KronrodintegralArrays.k15t[j2];
                fsum = f(center - t) + f(center + t);
                g7 += fsum * KronrodintegralArrays.g7w[j];
                k15 += fsum * KronrodintegralArrays.k15w[j2];
            }

            // calculate other half of k15
            for (j2 = 1; j2 < 8; j2 += 2)
            {
                t = halflength * KronrodintegralArrays.k15t[j2];
                fsum = f(center - t) + f(center + t);
                k15 += fsum * KronrodintegralArrays.k15w[j2];
            }

            // multiply by (a - b) / 2
            g7 = halflength * g7;
            k15 = halflength * k15;

            // 15 more function evaluations have been used
            increaseNumberOfEvaluations(15);

            // error is <= k15 - g7
            // if error is larger than tolerance then split the interval
            // in two and integrate recursively
            if (System.Math.Abs(k15 - g7) < tolerance)
            {
                return k15;
            }
            else
            {
                Utils.QL_REQUIRE(numberOfEvaluations() + 30 <= maxEvaluations(), () =>
                    "maximum number of function evaluations " + "exceeded", QLNetExceptionEnum.MaxNumberFuncEvalExceeded);
                return integrateRecursively(f, a, center, tolerance / 2) + integrateRecursively(f, center, b, tolerance / 2);
            }
        }
    }
}