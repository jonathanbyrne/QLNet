using JetBrains.Annotations;

namespace QLNet.Math
{
    /// <summary>
    ///     Ceiling truncation.
    /// </summary>
    [PublicAPI]
    public class CeilingTruncation : Rounding
    {
        public CeilingTruncation(int precision) : base(precision, Type.Ceiling, 5)
        {
        }

        public CeilingTruncation(int precision, int digit) : base(precision, Type.Ceiling, digit)
        {
        }
    }
}
