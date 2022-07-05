using System;
using QLNet.Time;

namespace QLNet
{
    public static partial class Utils
    {
        public static BusinessDayConvention euriborConvention(Period p)
        {
            switch (p.units())
            {
                case TimeUnit.Days:
                case TimeUnit.Weeks:
                    return BusinessDayConvention.Following;
                case TimeUnit.Months:
                case TimeUnit.Years:
                    return BusinessDayConvention.ModifiedFollowing;
                default:
                    throw new ArgumentException("Unknown TimeUnit: " + p.units());
            }
        }

        public static bool euriborEOM(Period p)
        {
            switch (p.units())
            {
                case TimeUnit.Days:
                case TimeUnit.Weeks:
                    return false;
                case TimeUnit.Months:
                case TimeUnit.Years:
                    return true;
                default:
                    throw new ArgumentException("Unknown TimeUnit: " + p.units());
            }
        }
    }
}
