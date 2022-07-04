namespace QLNet
{
    public static partial class Utils
    {
        public static double Asinh(double x) => System.Math.Log(x + System.Math.Sqrt(x * x + 1.0));
    }
}