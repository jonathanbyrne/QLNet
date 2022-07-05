using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public class DiscreteSimpsonIntegral
    {
        public double value(Vector x, Vector f)
        {
            var n = f.size();
            Utils.QL_REQUIRE(n == x.size(), () => "inconsistent size");

            double acc = 0;

            for (var j = 0; j < n - 2; j += 2)
            {
                var dxj = x[j + 1] - x[j];
                var dxjp1 = x[j + 2] - x[j + 1];

                var alpha = -dxjp1 * (2 * x[j] - 3 * x[j + 1] + x[j + 2]);
                var dd = x[j + 2] - x[j];
                var k = dd / (6 * dxjp1 * dxj);
                var beta = dd * dd;
                var gamma = dxj * (x[j] - 3 * x[j + 1] + 2 * x[j + 2]);

                acc += k * alpha * f[j] + k * beta * f[j + 1] + k * gamma * f[j + 2];
            }

            if (!((n & 1) == 1))
            {
                acc += 0.5 * (x[n - 1] - x[n - 2]) * (f[n - 1] + f[n - 2]);
            }

            return acc;
        }
    }
}
