using System.Collections.Generic;
using QLNet.Math.Interpolations;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class PiecewiseZeroSpreadedTermStructure : InterpolatedPiecewiseZeroSpreadedTermStructure<Linear>
    {
        public PiecewiseZeroSpreadedTermStructure(Handle<YieldTermStructure> h,
            List<Handle<Quote>> spreads,
            List<Date> dates,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.NoFrequency,
            DayCounter dc = default)
            : base(h, spreads, dates, compounding, frequency, dc, new Linear())
        { }
    }
}