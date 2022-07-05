using System;
using JetBrains.Annotations;

namespace QLNet.Math.integrals
{
    [PublicAPI]
    public interface IIntegrationPolicy
    {
        double integrate(Func<double, double> f, double a, double b, double I, int N);

        int nbEvalutions();
    }
}
