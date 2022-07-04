namespace QLNet.Cashflows
{
    public enum InterpolationType
    {
        AsIndex,   //!< same interpolation as index
        Flat,      //!< flat from previous fixing
        Linear     //!< linearly between bracketing fixings
    }
}