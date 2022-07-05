using QLNet.Time;

namespace QLNet.Termstructures.Volatility.equityfx
{
    public abstract class BlackVolatilityTermStructure : BlackVolTermStructure
    {
        /*! Returns the variance for the given strike and date calculating it
            from the volatility.
        */
        protected override double blackVarianceImpl(double maturity, double strike)
        {
            var vol = blackVolImpl(maturity, strike);
            return vol * vol * maturity;
        }

        #region Constructors

        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */

        protected BlackVolatilityTermStructure(BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(bdc, dc)
        {
        }

        //! initialize with a fixed reference date
        protected BlackVolatilityTermStructure(Date referenceDate, Calendar cal = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
            : base(referenceDate, cal, bdc, dc)
        {
        }

        //! calculate the reference date based on the global evaluation date
        protected BlackVolatilityTermStructure(int settlementDays, Calendar cal,
            BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
            : base(settlementDays, cal, bdc, dc)
        {
        }

        #endregion
    }
}
