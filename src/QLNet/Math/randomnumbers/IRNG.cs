using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Methods.montecarlo;

namespace QLNet.Math.RandomNumbers
{
    [PublicAPI]
    public interface IRNG
    {
        int dimension();

        IRNG factory(int dimensionality, ulong seed);

        Sample<List<double>> lastSequence();

        Sample<List<double>> nextSequence();
    }
}
