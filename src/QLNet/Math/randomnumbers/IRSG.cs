using JetBrains.Annotations;

namespace QLNet.Math.RandomNumbers
{
    [PublicAPI]
    public interface IRSG
    {
        int allowsErrorEstimate { get; }

        IRNG make_sequence_generator(int dimension, ulong seed);
    }
}
