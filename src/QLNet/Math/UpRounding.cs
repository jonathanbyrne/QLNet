using JetBrains.Annotations;

namespace QLNet.Math
{
    /// <summary>
    ///     Up-rounding
    /// </summary>
    [PublicAPI]
    public class UpRounding : Rounding
    {
        public UpRounding(int precision) : base(precision, Type.Up, 5)
        {
        }

        public UpRounding(int precision, int digit) : base(precision, Type.Up, digit)
        {
        }
    }
}
