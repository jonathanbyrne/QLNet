namespace QLNet.Math
{
    /// <summary>
    /// Down-rounding.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class DownRounding : Rounding
    {
        public DownRounding(int precision) : base(precision, Type.Down, 5) { }
        public DownRounding(int precision, int digit) : base(precision, Type.Down, digit) { }
    }
}