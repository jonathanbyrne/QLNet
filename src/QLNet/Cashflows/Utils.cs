using QLNet.Extensions;

namespace QLNet
{
    public static partial class Utils
    {
        public static double? toNullable(double? val)
        {
            if (val.IsEqual(double.MinValue) || val == null)
                return null;
            return val;
        }
    }
}