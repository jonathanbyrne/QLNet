using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes.Ibor;
using QLNet.Math;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class CCTEU : FloatingRateBond
    {
        public CCTEU(Date maturityDate, double spread, Handle<YieldTermStructure> fwdCurve = null,
            Date startDate = null, Date issueDate = null)
            : base(2, 100.0,
                new Schedule(startDate,
                    maturityDate, new Period(6, TimeUnit.Months),
                    new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                    DateGeneration.Rule.Backward, true),
                new Euribor6M(fwdCurve ?? new Handle<YieldTermStructure>()),
                new Actual360(),
                BusinessDayConvention.Following,
                new Euribor6M().fixingDays(),
                new List<double> { 1.0 }, // gearing
                new List<double> { spread },
                new List<double?>(), // caps
                new List<double?>(), // floors
                false, // in arrears
                100.0, // redemption
                issueDate)
        {
        }

        #region Bond interface

        //! accrued amount at a given date
        /*! The default bond settlement is used if no date is given. */
        public override double accruedAmount(Date d = null)
        {
            var result = base.accruedAmount(d);
            return new ClosestRounding(5).Round(result);
        }

        #endregion
    }
}
