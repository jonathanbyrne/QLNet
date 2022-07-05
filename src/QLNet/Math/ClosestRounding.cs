using JetBrains.Annotations;

namespace QLNet.Math
{
    /// <summary>
    ///     Closest rounding.
    /// </summary>
    [PublicAPI]
    public class ClosestRounding : Rounding
    {
        public ClosestRounding(int precision) : base(precision, Type.Closest, 5)
        {
        }

        public ClosestRounding(int precision, int digit) : base(precision, Type.Closest, digit)
        {
        }
    }
}
