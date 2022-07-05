namespace QLNet
{
    public enum Compounding
    {
        Simple = 0, //!< \f$ 1+rt \f$
        Compounded = 1, //!< \f$ (1+r)^t \f$
        Continuous = 2, //!< \f$ e^{rt} \f$
        SimpleThenCompounded //!< Simple up to the first period then Compounded
    }
}
