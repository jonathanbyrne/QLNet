using JetBrains.Annotations;

namespace QLNet.Math
{
    /// <summary>
    ///     Floor truncation.
    /// </summary>
    [PublicAPI]
    public class FloorTruncation : Rounding
    {
        public FloorTruncation(int precision) : base(precision, Type.Floor, 5)
        {
        }

        public FloorTruncation(int precision, int digit) : base(precision, Type.Floor, digit)
        {
        }
    }
}
