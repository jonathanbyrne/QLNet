using System;
using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class GaussKronrodNonAdaptive : Integrator
    {
        private double relativeAccuracy_;

        public GaussKronrodNonAdaptive(double absoluteAccuracy, int maxEvaluations, double relativeAccuracy) : base(absoluteAccuracy, maxEvaluations)
        {
            relativeAccuracy_ = relativeAccuracy;
        }

        public double relativeAccuracy() => relativeAccuracy_;

        protected override double integrate(Func<double, double> f, double a, double b)
        {
            double result;
            var fv1 = new double[5];
            var fv2 = new double[5];
            var fv3 = new double[5];
            var fv4 = new double[5];
            var savfun = new double[21]; // array of function values which have been computed
            double res10; // 10, 21, 43 and 87 point results
            double res21;
            double res43;
            double res87;
            double err;
            double resAbs; // approximation to the integral of abs(f)
            double resasc; // approximation to the integral of abs(f-i/(b-a))
            int k;

            QLNet.Utils.QL_REQUIRE(a < b, () => "b must be greater than a)");

            var halfLength = 0.5 * (b - a);
            var center = 0.5 * (b + a);
            var fCenter = f(center);

            // Compute the integral using the 10- and 21-point formula.

            res10 = 0;
            res21 = KronrodintegralArrays.w21b[5] * fCenter;
            resAbs = KronrodintegralArrays.w21b[5] * System.Math.Abs(fCenter);

            for (k = 0; k < 5; k++)
            {
                var abscissa = halfLength * KronrodintegralArrays.x1[k];
                var fval1 = f(center + abscissa);
                var fval2 = f(center - abscissa);
                var fval = fval1 + fval2;
                res10 += KronrodintegralArrays.w10[k] * fval;
                res21 += KronrodintegralArrays.w21a[k] * fval;
                resAbs += KronrodintegralArrays.w21a[k] * (System.Math.Abs(fval1) + System.Math.Abs(fval2));
                savfun[k] = fval;
                fv1[k] = fval1;
                fv2[k] = fval2;
            }

            for (k = 0; k < 5; k++)
            {
                var abscissa = halfLength * KronrodintegralArrays.x2[k];
                var fval1 = f(center + abscissa);
                var fval2 = f(center - abscissa);
                var fval = fval1 + fval2;
                res21 += KronrodintegralArrays.w21b[k] * fval;
                resAbs += KronrodintegralArrays.w21b[k] * (System.Math.Abs(fval1) + System.Math.Abs(fval2));
                savfun[k + 5] = fval;
                fv3[k] = fval1;
                fv4[k] = fval2;
            }

            result = res21 * halfLength;
            resAbs *= halfLength;
            var mean = 0.5 * res21;
            resasc = KronrodintegralArrays.w21b[5] * System.Math.Abs(fCenter - mean);

            for (k = 0; k < 5; k++)
            {
                resasc += KronrodintegralArrays.w21a[k] * (System.Math.Abs(fv1[k] - mean) + System.Math.Abs(fv2[k] - mean)) + KronrodintegralArrays.w21b[k] * (System.Math.Abs(fv3[k] - mean) + System.Math.Abs(fv4[k] - mean));
            }

            err = KronrodintegralArrays.rescaleError((res21 - res10) * halfLength, resAbs, resasc);
            resasc *= halfLength;

            // test for convergence.
            if (err < absoluteAccuracy() || err < relativeAccuracy() * System.Math.Abs(result))
            {
                setAbsoluteError(err);
                setNumberOfEvaluations(21);
                return result;
            }

            // compute the integral using the 43-point formula.

            res43 = KronrodintegralArrays.w43b[11] * fCenter;

            for (k = 0; k < 10; k++)
            {
                res43 += savfun[k] * KronrodintegralArrays.w43a[k];
            }

            for (k = 0; k < 11; k++)
            {
                var abscissa = halfLength * KronrodintegralArrays.x3[k];
                var fval = f(center + abscissa) + f(center - abscissa);
                res43 += fval * KronrodintegralArrays.w43b[k];
                savfun[k + 10] = fval;
            }

            // test for convergence.

            result = res43 * halfLength;
            err = KronrodintegralArrays.rescaleError((res43 - res21) * halfLength, resAbs, resasc);

            if (err < absoluteAccuracy() || err < relativeAccuracy() * System.Math.Abs(result))
            {
                setAbsoluteError(err);
                setNumberOfEvaluations(43);
                return result;
            }

            // compute the integral using the 87-point formula.

            res87 = KronrodintegralArrays.w87b[22] * fCenter;

            for (k = 0; k < 21; k++)
            {
                res87 += savfun[k] * KronrodintegralArrays.w87a[k];
            }

            for (k = 0; k < 22; k++)
            {
                var abscissa = halfLength * KronrodintegralArrays.x4[k];
                res87 += KronrodintegralArrays.w87b[k] * (f(center + abscissa) + f(center - abscissa));
            }

            // test for convergence.
            result = res87 * halfLength;
            err = KronrodintegralArrays.rescaleError((res87 - res43) * halfLength, resAbs, resasc);

            setAbsoluteError(err);
            setNumberOfEvaluations(87);
            return result;
        }
    }
}
