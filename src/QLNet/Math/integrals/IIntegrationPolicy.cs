using System;

namespace QLNet.Math.integrals
{
    [JetBrains.Annotations.PublicAPI] public interface IIntegrationPolicy
    {
        double integrate(Func<double, double> f, double a, double b, double I, int N);
        int nbEvalutions();
    }
}