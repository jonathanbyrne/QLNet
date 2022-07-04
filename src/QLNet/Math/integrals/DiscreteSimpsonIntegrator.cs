using System;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public class DiscreteSimpsonIntegrator : Integrator
    {
        public DiscreteSimpsonIntegrator(int evaluations)
            : base(null, evaluations)
        { }

        protected override double integrate(Func<double, double> f, double a, double b)
        {
            var x = new Vector(maxEvaluations(), a, (b - a) / (maxEvaluations() - 1));
            var fv = new Vector(x.size());
            x.ForEach((g, gg) => fv[g] = f(gg));

            increaseNumberOfEvaluations(maxEvaluations());
            return new DiscreteSimpsonIntegral().value(x, fv);
        }
    }
}