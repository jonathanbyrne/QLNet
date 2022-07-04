namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public interface IRSG
    {
        int allowsErrorEstimate { get; }
        IRNG make_sequence_generator(int dimension, ulong seed);
    }
}