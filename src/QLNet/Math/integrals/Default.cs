using System;

namespace QLNet.Math.integrals
{
    public struct Default : IIntegrationPolicy
    {
        public double integrate(Func<double, double> f, double a, double b, double I, int N)
        {
            var sum = 0.0;
            var dx = (b - a) / N;
            var x = a + dx / 2.0;
            for (var i = 0; i < N; x += dx, ++i)
                sum += f(x);
            return (I + dx * sum) / 2.0;
        }
        public int nbEvalutions() => 2;
    }
}