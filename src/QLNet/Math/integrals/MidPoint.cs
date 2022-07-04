using System;

namespace QLNet.Math.integrals
{
    public struct MidPoint : IIntegrationPolicy
    {
        public double integrate(Func<double, double> f, double a, double b, double I, int N)
        {
            var sum = 0.0;
            var dx = (b - a) / N;
            var x = a + dx / 6.0;
            var D = 2.0 * dx / 3.0;
            for (var i = 0; i < N; x += dx, ++i)
                sum += f(x) + f(x + D);
            return (I + dx * sum) / 3.0;
        }

        public int nbEvalutions() => 3;
    }
}