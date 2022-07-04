using System;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class DatedOISRateHelper : RateHelper
    {

        public DatedOISRateHelper(Date startDate,
            Date endDate,
            Handle<Quote> fixedRate,
            OvernightIndex overnightIndex)

            : base(fixedRate)
        {

            overnightIndex.registerWith(update);

            // dummy OvernightIndex with curve/swap arguments
            // review here
            IborIndex clonedIborIndex = overnightIndex.clone(termStructureHandle_);
            var clonedOvernightIndex = clonedIborIndex as OvernightIndex;

            swap_ = new MakeOIS(new Period(), clonedOvernightIndex, 0.0)
                .withEffectiveDate(startDate)
                .withTerminationDate(endDate)
                .withDiscountingTermStructure(termStructureHandle_);

            earliestDate_ = swap_.startDate();
            latestDate_ = swap_.maturityDate();

        }


        public override void setTermStructure(YieldTermStructure t)
        {
            // no need to register---the index is not lazy
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);

        }

        public override double impliedQuote()
        {
            if (termStructure_ == null)
                throw new ArgumentException("term structure not set");

            // we didn't register as observers - force calculation
            swap_.recalculate();
            return swap_.fairRate().Value;
        }

        protected OvernightIndexedSwap swap_;
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
    }
}