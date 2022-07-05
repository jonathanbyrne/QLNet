using JetBrains.Annotations;

namespace QLNet.Math.randomnumbers
{
    [PublicAPI]
    public interface IRSG
    {
        int allowsErrorEstimate { get; }

        IRNG make_sequence_generator(int dimension, ulong seed);
    }
}
