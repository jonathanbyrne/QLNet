using QLNet.Extensions;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.equityfx
{
    public abstract class BlackVarianceTermStructure : BlackVolTermStructure
    {
        #region Constructors
        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */

        protected BlackVarianceTermStructure(BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(bdc, dc)
        { }

        //! initialize with a fixed reference date
        protected BlackVarianceTermStructure(Date referenceDate, Calendar cal = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
            : base(referenceDate, cal, bdc, dc)
        { }

        //! calculate the reference date based on the global evaluation date
        protected BlackVarianceTermStructure(int settlementDays, Calendar cal,
            BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null)
            : base(settlementDays, cal, bdc, dc)
        { }

        #endregion

        /*! Returns the volatility for the given strike and date calculating it
            from the variance.
        */
        protected override double blackVolImpl(double t, double strike)
        {
            var nonZeroMaturity = t.IsEqual(0.0) ? 0.00001 : t;
            var var = blackVarianceImpl(nonZeroMaturity, strike);
            return System.Math.Sqrt(var / nonZeroMaturity);
        }

    }
}