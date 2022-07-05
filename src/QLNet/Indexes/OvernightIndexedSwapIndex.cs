using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Instruments;
using QLNet.Time;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class OvernightIndexedSwapIndex : SwapIndex
    {
        protected new Date lastFixingDate_;
        // cache data to avoid swap recreation when the same fixing date
        // is used multiple time to forecast changing fixing
        protected new OvernightIndexedSwap lastSwap_;
        protected OvernightIndex overnightIndex_;

        public OvernightIndexedSwapIndex(string familyName,
            Period tenor,
            int settlementDays,
            Currency currency,
            OvernightIndex overnightIndex)
            : base(familyName, tenor, settlementDays,
                currency, overnightIndex.fixingCalendar(),
                new Period(1, TimeUnit.Years), BusinessDayConvention.ModifiedFollowing,
                overnightIndex.dayCounter(), overnightIndex)
        {
            overnightIndex_ = overnightIndex;
        }

        // Inspectors
        public OvernightIndex overnightIndex() => overnightIndex_;

        /*! \warning Relinking the term structure underlying the index will
                     not have effect on the returned swap.
        */
        public new OvernightIndexedSwap underlyingSwap(Date fixingDate)
        {
            Utils.QL_REQUIRE(fixingDate != null, () => "null fixing date");
            if (lastFixingDate_ != fixingDate)
            {
                var fixedRate = 0.0;
                lastSwap_ = new MakeOIS(tenor_, overnightIndex_, fixedRate)
                    .withEffectiveDate(valueDate(fixingDate))
                    .withFixedLegDayCount(dayCounter_);
                lastFixingDate_ = fixingDate;
            }

            return lastSwap_;
        }
    }
}
