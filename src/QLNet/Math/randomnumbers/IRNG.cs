using System.Collections.Generic;
using QLNet.Methods.montecarlo;

namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public interface IRNG
    {
        int dimension();
        Sample<List<double>> nextSequence();
        Sample<List<double>> lastSequence();

        IRNG factory(int dimensionality, ulong seed);
    }
}